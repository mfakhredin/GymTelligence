using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymTelligence.Client.Auth;

namespace GymTelligence.Client.Services;

public sealed class ApiClient(HttpClient http, SessionState session)
{
    public async Task<T> GetAsync<T>(string url)
    {
        await session.EnsureInitializedAsync();
        return await SendAsync<T>(() => http.GetAsync(url));
    }

    public async Task<T> PostAsync<T>(string url, object? value = null)
    {
        await session.EnsureInitializedAsync();
        return await SendAsync<T>(() => value is null
            ? http.PostAsync(url, null)
            : http.PostAsJsonAsync(url, value, JsonOptions.Default));
    }

    public async Task PostAsync(string url, object? value = null)
    {
        await session.EnsureInitializedAsync();
        await SendAsync(() => value is null
            ? http.PostAsync(url, null)
            : http.PostAsJsonAsync(url, value, JsonOptions.Default));
    }

    public async Task<T> PutAsync<T>(string url, object value)
    {
        await session.EnsureInitializedAsync();
        return await SendAsync<T>(() => http.PutAsJsonAsync(url, value, JsonOptions.Default));
    }

    public async Task PutAsync(string url, object value)
    {
        await session.EnsureInitializedAsync();
        await SendAsync(() => http.PutAsJsonAsync(url, value, JsonOptions.Default));
    }

    public async Task DeleteAsync(string url)
    {
        await session.EnsureInitializedAsync();
        await SendAsync(() => http.DeleteAsync(url));
    }

    private async Task<T> SendAsync<T>(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            using var response = await send();
            return await ReadAsync<T>(response);
        }
        catch (ApiException) { throw; }
        catch (TaskCanceledException) { throw new ApiException("The server took too long to respond. Please try again."); }
        catch (HttpRequestException) { throw new ApiException("The server is unavailable right now. Check your connection and try again."); }
    }

    private async Task SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            using var response = await send();
            await EnsureSuccessAsync(response);
        }
        catch (ApiException) { throw; }
        catch (TaskCanceledException) { throw new ApiException("The server took too long to respond. Please try again."); }
        catch (HttpRequestException) { throw new ApiException("The server is unavailable right now. Check your connection and try again."); }
    }

    private async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions.Default)
            ?? throw new ApiException("The server returned an empty response.");
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await session.SignOutAsync();
            throw new ApiException("Your session expired. Please log in again.");
        }
        var message = response.StatusCode == HttpStatusCode.TooManyRequests ? "Too many requests. Take a breath and try again shortly." : null;
        try
        {
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            if (json.RootElement.TryGetProperty("title", out var title)) message = title.GetString();
            if (json.RootElement.TryGetProperty("detail", out var detail) && !string.IsNullOrWhiteSpace(detail.GetString())) message = detail.GetString();
            if (json.RootElement.TryGetProperty("errors", out var errors))
                message = string.Join(" ", errors.EnumerateObject().SelectMany(x => x.Value.ValueKind == JsonValueKind.Array
                    ? x.Value.EnumerateArray().Select(value => value.GetString())
                    : [x.Value.GetString()]));
        }
        catch { }
        throw new ApiException(message ?? "The request could not be completed.");
    }
}

public sealed class ApiException(string message) : Exception(message);

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web);
}
