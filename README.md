# GWS Workforce Web

GWS Workforce Web is a secure ASP.NET Core Blazor Server application for user-scoped AI workforce interactions backed by local Ollama models.

## Overview

The application provides:

- Authenticated user accounts with ASP.NET Core Identity
- Private per-user conversations with AI workers
- Private per-user knowledge items used to enrich interactions
- Local model inference through Ollama
- SQLite persistence through Entity Framework Core

## Tech Stack

- .NET 10
- ASP.NET Core Blazor Server
- Entity Framework Core (SQLite)
- ASP.NET Core Identity
- xUnit + bUnit tests

## Project Structure

- `Components/`: Blazor UI and layouts
- `Application/`: service contracts and app models
- `Infrastructure/`: service implementations and data access behavior
- `Data/`: DbContext, identity user model, migrations, seed data
- `Models/`: workforce entities
- `tests/Application.Tests/`: service and UI component tests

## Local Prerequisites

- .NET SDK 10.x
- Ollama running locally at `http://localhost:11434`

## Setup and Run

1. Restore packages:

```bash
dotnet restore GwsWorkforce.Web.csproj
```

2. Apply database migrations:

```bash
dotnet ef database update
```

3. Run the app:

```bash
dotnet run --project GwsWorkforce.Web.csproj
```

4. Open the app in the browser using the local URL shown in terminal output.

## Testing

Run the automated tests with:

```bash
dotnet test tests/Application.Tests/Application.Tests.csproj
```

## CI Quality Gate

The CI workflow enforces:

- Repository hygiene (no tracked generated files)
- Restore and build with warnings as errors
- Test project discovery and execution
- Migration safety check

## Security and Privacy

- User data access is scoped by authenticated user ID
- Identity-based authentication and authorization are required
- Dependency vulnerability scanning is part of release readiness

## Notes

- Build artifacts and local DB files are ignored via `.gitignore`.
- Only this root `README.md` is intended for public markdown documentation.
