# Repository Guidelines

## Project Structure & Module Organization
- `AppBackend.ApiCore/`: ASP.NET Core entry point (controllers, middleware, settings) plus `appsettings.*` configs.
- `AppBackend.Services/`: Business services, background jobs, helpers, AutoMapper profiles, API DTOs.
- `AppBackend.Repositories/`: EF Core repositories, unit of work, generic repository base.
- `AppBackend.BusinessObjects/`: Entities, DTOs, enums, constants, migrations, typed app settings.
- `AppBackend.Tests/`: xUnit test suite with helpers and service tests.
- Docs at root (`README.md`, `API_DOCS_ROOM_MEDIA_CRUD.md`, etc.) cover feature-specific flows.

## Build, Test, and Development Commands
- Restore deps: `dotnet restore AppBackend.sln`
- Build solution: `dotnet build AppBackend.sln -c Debug` (use `Release` for Docker/publish)
- Run API locally: `dotnet run --project AppBackend.ApiCore/AppBackend.ApiCore.csproj --urls http://localhost:8080`
- Run tests: `dotnet test AppBackend.Tests/AppBackend.Tests.csproj -c Debug`
- Optional coverage: `dotnet test AppBackend.Tests/AppBackend.Tests.csproj /p:CollectCoverage=true`

## Coding Style & Naming Conventions
- C# 12 / .NET 9 with nullable + implicit usings enabled; use 4-space indentation and trailing commas sparingly.
- PascalCase for classes/interfaces/namespaces; camelCase for locals/fields (prefix underscores for injected fields); suffix async methods with `Async`.
- Keep controllers slim—delegate to services; persist via repositories/UnitOfWork; map DTOs with AutoMapper profiles in `AppBackend.Services/Mappings`.
- Prefer structured logging (`_logger.LogInformation("... {Value}", value)`), and return typed `ResultModel` responses used across services.

## Testing Guidelines
- Frameworks: xUnit + FluentAssertions + Moq; EF Core InMemory for data-heavy tests.
- Place tests under `AppBackend.Tests` mirroring service namespaces; name files `*Tests.cs` and methods `MethodUnderTest_State_Expectation`.
- Run `dotnet test` before commits; add regression cases when touching controllers/services or Room/Booking flows.
- Use `TestDataSeeder` helpers for repeatable setups; keep assertions focused on status codes, payload shapes, and side effects.

## Commit & Pull Request Guidelines
- Commit messages follow short imperative style (e.g., “Refactor room media handling…”); group related changes per commit.
- PRs should include: concise summary, linked issue/task, key endpoints affected, and test evidence (`dotnet test` output). Add screenshots/JSON samples for API behavior changes.
- Keep changes small and scoped; avoid formatting-only diffs in feature PRs; ensure new configs or migrations are called out in the description.

## Configuration & Security Tips
- Start from `AppBackend.ApiCore/appsettings.template.json`; copy to `appsettings.Development.json` with local secrets. Never commit real keys (JWT, Cloudinary, PayOS, Google, connection strings).
- Default connection targets SQL Server; update `ConnectionStrings:DefaultConnection` per environment. For containers, ensure ports match the `Dockerfile` (8080/8081).
- Enable optional Google auth by supplying `GoogleAuth` settings and uncommenting the registration in `Program.cs`.
