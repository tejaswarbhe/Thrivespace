# Startup Incubation Management System — Backend

Modular monolith, ASP.NET Core 8 + MySQL (Pomelo EF Core provider) + JWT.

## Structure

- **StartupIMS.Domain** — entities and enums only, zero dependencies
- **StartupIMS.Shared** — JWT settings, DTOs
- **StartupIMS.Infrastructure** — `IdentityDbContext` (Users, RefreshTokens) and `CoreDbContext`
  (Startups, Mentors, MentorAssignments, Applications, ProgressReports, Funding), JWT token service
- **StartupIMS.API** — controllers, `Program.cs`, auth wiring

Two `DbContext`s on purpose, even on one MySQL server: this is the seam to cut along
when Identity gets extracted into its own service later. No entity crosses that
boundary with an EF navigation property — only a plain `UserId` foreign key.

## Before running

1. `dotnet restore` (needs NuGet access — not available in this sandbox, so this
   wasn't built/tested here; run it on your machine)
2. Create two MySQL databases: `startupims_identity`, `startupims_core`
3. Set real secrets — **do not commit** `appsettings.json` as-is:
   ```
   dotnet user-secrets init --project src/StartupIMS.API
   dotnet user-secrets set "Jwt:Secret" "<a long random string>" --project src/StartupIMS.API
   dotnet user-secrets set "ConnectionStrings:IdentityDb" "..." --project src/StartupIMS.API
   dotnet user-secrets set "ConnectionStrings:CoreDb" "..." --project src/StartupIMS.API
   ```
4. Add migrations for each context:
   ```
   dotnet ef migrations add InitialCreate --project src/StartupIMS.Infrastructure --startup-project src/StartupIMS.API --context IdentityDbContext -o Migrations/Identity
   dotnet ef migrations add InitialCreate --project src/StartupIMS.Infrastructure --startup-project src/StartupIMS.API --context CoreDbContext -o Migrations/Core
   dotnet ef database update --context IdentityDbContext --project src/StartupIMS.Infrastructure --startup-project src/StartupIMS.API
   dotnet ef database update --context CoreDbContext --project src/StartupIMS.Infrastructure --startup-project src/StartupIMS.API
   ```
5. `dotnet run --project src/StartupIMS.API` and open `/swagger`

## What's implemented

- `AuthController`: register / login / refresh (JWT access + rotating refresh tokens)
- Role-based policies: `AdminOnly`, `MentorOnly`, `FounderOnly`, `AdminOrMentor`
- `StartupsController`: demonstrates the 3-role access pattern (Admin sees all,
  Mentor sees assigned startups, Founder sees only their own)
- `MentorsController`: stub for Admin-managed mentor assignment

## Still to build out

- `ApplicationsController`, `ProgressReportsController`, `FundingController` (same
  pattern as `StartupsController` — copy and adapt)
- EF migrations (can't be generated in this sandbox — no `dotnet` CLI / NuGet access)
- Password/email validation, FluentValidation or DataAnnotations on DTOs
- Logger: still undecided in our conversation — Serilog is the standard pick for
  ASP.NET Core, wire it in `Program.cs` when you decide
- The Python analytics service comes later, as a separate service the API calls
  into (or that reads from `CoreDb` directly) — doesn't need to be built yet
