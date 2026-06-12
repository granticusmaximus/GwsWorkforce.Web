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

## Docker

Build the image:

```bash
docker build -t gws-workforce-web .
```

Run it with the default SQLite path persisted to a local volume and Ollama pointed at a reachable base URL:

```bash
docker run -d \
  --name gws-workforce-web \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:8080 \
  -e Ollama__BaseUrl=http://host.docker.internal:11434 \
  -v gws-workforce-data:/app/Data \
  gws-workforce-web
```

On Linux hosts, `host.docker.internal` requires Docker's host-gateway support. The included `compose.yaml` already configures that mapping.
If Ollama is running on the same Linux host outside Docker, configure Ollama with `OLLAMA_HOST=0.0.0.0:11434` and keep port `11434` closed at the cloud firewall unless you explicitly need direct remote API access.

## Docker Compose

The repository now includes `compose.yaml` for a single-container web deployment:

```bash
docker compose up -d --build
```

Override the Ollama endpoint as needed:

```bash
export OLLAMA_BASE_URL=http://your-ollama-host:11434
docker compose up -d --build
```

## Deployment Notes

- The app now reads `Ollama:BaseUrl` from configuration, defaulting to `http://localhost:11434`.
- If the web container and Ollama run on the same Linux host, keep Ollama private and point the app at the host or an internal network address.
- If Ollama runs elsewhere, set `Ollama__BaseUrl` to that private or tunneled endpoint.
- A DigitalOcean-focused deployment walkthrough is in `docs/DigitalOcean-Deployment.md`.

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
- Enterprise readiness tracking plan: `docs/Enterprise-Readiness-Plan.md`.

## Delivery Status

- Phase 2 (UX/UI Professionalization): Closed
	- Visual shell and reusable components for Workforce and Knowledge pages are implemented.
	- Accessibility checks cover ARIA labels and keyboard-reachable controls.
	- Visual regression baselines are covered by UI signature tests for default and validation states.
- Phase 3 (Backend Completion and Data Integrity): Started in test-first mode
	- Initial data integrity tests are in place for deterministic paging and ordering behavior.
