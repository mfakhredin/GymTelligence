using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using GymTelligence.Client.Models;
using GymTelligence.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace GymTelligence.Client.Auth;

public sealed class SessionState(IJSRuntime js, HttpClient http) : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private bool _initialized;
    public SessionData? Current { get; private set; }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await EnsureInitializedAsync();
        return new AuthenticationState(ToPrincipal(Current));
    }

    public async Task EnsureInitializedAsync()
    {
        if (_initialized) return;
        _initialized = true;
        try
        {
            var json = await js.InvokeAsync<string?>("gymSession.get");
            if (!string.IsNullOrWhiteSpace(json)) Current = JsonSerializer.Deserialize<SessionData>(json, JsonOptions.Default);
        }
        catch { Current = null; }
        ApplyToken();
    }

    public async Task SignInAsync(AuthResponse response)
    {
        Current = new SessionData(response.Token, response.User);
        _initialized = true;
        ApplyToken();
        await js.InvokeVoidAsync("gymSession.set", JsonSerializer.Serialize(Current, JsonOptions.Default));
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(ToPrincipal(Current))));
    }

    public async Task SignOutAsync()
    {
        Current = null;
        http.DefaultRequestHeaders.Authorization = null;
        await js.InvokeVoidAsync("gymSession.clear");
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }

    public void UpdateUser(UserModel user)
    {
        if (Current is null) return;
        Current = Current with { User = user };
        _ = js.InvokeVoidAsync("gymSession.set", JsonSerializer.Serialize(Current, JsonOptions.Default));
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(ToPrincipal(Current))));
    }

    private void ApplyToken() => http.DefaultRequestHeaders.Authorization = Current is null ? null : new AuthenticationHeaderValue("Bearer", Current.Token);
    private static ClaimsPrincipal ToPrincipal(SessionData? value) => value is null ? Anonymous : new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, value.User.Id.ToString()), new Claim(ClaimTypes.Name, value.User.Name), new Claim(ClaimTypes.Email, value.User.Email), new Claim(ClaimTypes.Role, value.User.Role)], "jwt"));
}

public sealed record SessionData(string Token, UserModel User);
