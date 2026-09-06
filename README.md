# GymTelligence

GymTelligence is a full-stack adaptive fitness platform rebuilt from the original PHP/MySQL application as a layered ASP.NET Core solution. It combines workout programming, set-level tracking, progress telemetry, reminders, role-based content management, an AI coach, and optional subscriptions in one responsive web app.

The client uses a dark performance-focused design with responsive desktop and mobile navigation, accessible keyboard states, clear retry screens, and a focused workout workspace. Set completion updates in place without reloading the full plan, keeping workout tracking fast and economical for the API.

## Stack

- ASP.NET Core 9 Web API and hosted Blazor WebAssembly client
- Entity Framework Core 9 with SQL Server / LocalDB
- JWT bearer authentication and role authorization
- Swagger / OpenAPI, Problem Details, rate limiting, and health checks
- Optional Gemini coaching, Stripe Checkout/webhooks, and SMTP password recovery
- xUnit service tests

## Solution structure

```text
Domain/                 Entities and enums
Application/            DTOs, contracts, validation, and business services
Infrastructure/         EF Core, authentication, email, Gemini, and Stripe adapters
GymTelligence/          API host, middleware, controllers, configuration, and migrations startup
GymTelligence.Client/   Responsive Blazor WebAssembly interface
GymTelligence.Tests/    Core service tests
```

## Run locally

Requirements: Visual Studio 2022 with the ASP.NET workload, .NET 9 SDK, and SQL Server LocalDB.

1. Open `GymTelligence.sln`.
2. Set the `GymTelligence` web project as the startup project.
3. Select its `https` profile and run it. If this is your first local ASP.NET project, trust the development certificate when Visual Studio asks.
4. Open `https://localhost:7247` if the browser does not open automatically.

The host applies the checked-in migration and seeds the exercise catalog, plan templates, badges, and scientific references at startup. Fixed demo accounts and sample progress data are created only in the Development environment.

Demo accounts:

| Role | Email | Password |
|---|---|---|
| Athlete | `demo@gymtelligence.test` | `password` |
| Trainer | `trainer@gymtelligence.test` | `trainer123` |
| Administrator | `admin@gymtelligence.com` | `admin123` |

Swagger is available at `https://localhost:7247/swagger` in Development. The health endpoint is `/health`.

## Private configuration

Do not commit live keys. The checked-in development configuration contains only a local database connection and a clearly marked development signing key. Use .NET user secrets locally or environment variables in deployment. Example values and field names are in `GymTelligence/appsettings.Local.example.json`.

```powershell
dotnet user-secrets set "Jwt:Key" "a-long-random-secret-of-at-least-32-bytes" --project GymTelligence
dotnet user-secrets set "Gemini:ApiKey" "your-key" --project GymTelligence
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..." --project GymTelligence
dotnet user-secrets set "Stripe:PriceId" "price_..." --project GymTelligence
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..." --project GymTelligence
```

Configure `ConnectionStrings:Default` for a non-LocalDB SQL Server. Configure the `Email:*` fields for production password-reset delivery. Without a Gemini key the coach returns a safe local fallback; without Stripe keys checkout remains disabled.

Production startup intentionally fails when a database connection or JWT signing key is missing. This prevents a deployment from silently using the public development settings.

For Stripe, register `POST /api/subscriptions/webhook` as the webhook endpoint. The backend processes checkout completion, subscription create/update/delete, and failed-payment events after signature verification.

## Verification

```powershell
dotnet build GymTelligence.sln
dotnet test GymTelligence.sln
```

This ASP.NET solution is a separate conversion of the original PHP application.
