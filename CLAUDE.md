# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

BornoBit Restaurant — a restaurant ordering + management system. Three runnable apps over one shared
SQL Server (LocalDB) database:

- **API** (`backend/src/BornoBit.Restaurant.Api`, port 5000) — REST/minimal-API for the customer flow + staff JSON auth.
- **Customer site** (`customer-web`, Next.js 15, port 3000) — phone-OTP login, menu, cart, checkout.
- **Staff console** (`backend/src/BornoBit.Restaurant.Web`, Blazor Server, port 5002) — cookie login, orders, admin, settings.

The architecture mirrors the reference project at `E:\ReportProject\HealthCareApp` (Clean Architecture + CQRS),
but **multi-tenancy was deliberately stripped** — see "Tenancy" below.

## Commands

Backend (run from `backend/`):
```
dotnet build BornoBit.Restaurant.slnx                         # build all 7 projects (note: .slnx, not .sln)
dotnet run --project src/BornoBit.Restaurant.Api              # API on :5000 (auto-migrates + seeds)
dotnet run --project src/BornoBit.Restaurant.Web             # Staff console on :5002
dotnet ef migrations add <Name> -p src/BornoBit.Restaurant.Infrastructure -s src/BornoBit.Restaurant.Api
dotnet ef database update      -p src/BornoBit.Restaurant.Infrastructure -s src/BornoBit.Restaurant.Api
```

Frontend (from `customer-web/`): `npm install` then `npm run dev` (:3000), `npm run build`, `npm run lint`.

MCP SQL server (from repo root): `dotnet build tools/BornoBit.Restaurant.Mcp` — built DLL is wired in `.mcp.json`.

There is **no test project** yet.

### Critical run gotchas
- **Staff console MUST run in Development** (plain `dotnet run` uses the launch profile → Development). Running with
  `--no-launch-profile` forces Production, where the Blazor component scoped-style bundle (`*.styles.css`) 404s and
  the whole UI renders unstyled. App.razor uses `UseStaticFiles()` + plain asset paths (not fingerprinted
  `MapStaticAssets`) precisely because the latter served empty/broken CSS here.
- **DB is LocalDB + Windows auth**: `Server=(localdb)\MSSQLLocalDB;Database=BornoBitRestaurant;Trusted_Connection=True`.
  This rules out Node (`tedious`) and most off-the-shelf MCP SQL servers — only `Microsoft.Data.SqlClient` (.NET) and
  ODBC reach LocalDB named pipes. The MCP server in `tools/` is custom .NET for this reason.
- Both API and Web call `db.Database.MigrateAsync()` + seeders on startup; whichever boots first applies migrations.

## Architecture

### Backend layering (Clean Architecture + CQRS via MediatR)
`Shared` ← `Domain` ← `Application` ← `Infrastructure` ← (`Api`, `Web`); `Reporting` ← `Domain`/`Shared`.

- **Domain** — entities use a private-ctor + static `Create()` factory pattern with private setters. Base classes in
  `Domain/Common`: `AuditableEntity` (audit + soft-delete, **no TenantId**) and `BaseEntity` (child/join rows). Money is
  `decimal(18,2)`; line totals are computed props marked `builder.Ignore(...)` in EF config.
- **Application** — one folder per feature; each file holds the MediatR request record + handler (+ FluentValidation
  validator). Pipeline behaviors (`ValidationBehavior`, `LoggingBehavior`, `UnhandledExceptionBehavior`) registered via
  `AddApplication()`. Queries return DTOs / `PagedResult<T>`. `IAppDbContext` exposes the DbSets handlers use.
- **Infrastructure** — `ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`, EF configs
  auto-applied from assembly, a **soft-delete-only** global query filter (no tenant filter), `AuditableEntityInterceptor`
  stamps audit fields off `ICurrentUser.UserName`. Seeders (`RoleSeeder`, `SuperAdminSeeder`, `MenuSeeder`, `TableSeeder`,
  `TenantSeeder`, `AppMenuSeeder`) are idempotent.
- **Reporting** — QuestPDF order-receipt PDFs. `IReportRenderer` consumed by both API (`/orders/{id}/receipt.pdf`) and
  Web (`/reports/order/{id}/receipt.pdf`).

### Handler registration split (important)
- **Api** calls `AddApplication()` only → registers handlers in the Application assembly. It does **not** scan
  Infrastructure, so the **user-management handlers (in `Infrastructure/Identity/UserCommandHandlers.cs`) are invisible to
  the API by design** — admin is Web-only.
- **Web** calls `AddApplication(typeof(Infrastructure.DependencyInjection).Assembly)` → registers both Application and
  Infrastructure handlers, so the admin pages work.

### Auth model (three distinct mechanisms)
- **Customer** (API): phone OTP. `RequestOtpCommand` upserts a `Customer` by phone and issues a code (returned in dev via
  `Otp:ReturnCodeInDev`); `VerifyOtpCommand` issues a JWT with `typ=customer` (audience `...Customer`). Policy `Customer`.
- **Staff (API)**: `POST /staff/auth/login` (SignInManager) → JWT `typ=staff` + role claims (audience `...Staff`). Policy `Staff`.
- **Staff (Web)**: cookie auth. `Endpoints/AccountEndpoints.cs` `POST /account/login` signs a `Cookies` principal. Pages use
  policies `Staff` / `Admin` / `SuperAdmin`.
- Roles: `SuperAdmin, Admin, Manager, Waiter, Chef, Cashier` (`Domain/Identity/Roles.cs`). Seeded super-admin
  `admin@bornobit.local` / `ChangeMe!2026`.

### Staff console UI
Imported design system from the reference: the `Hc` component library (`Web/Components/Hc/**`), shared components
(`Web/Components/Shared/**`), design tokens (`wwwroot/app.css` `--hc-*` vars) + runtime theme switch (`wwwroot/hc-theme.js`,
the Settings ThemePicker). **Navigation is DB-driven**: the sidebar is built from the `AppMenus` / `AppMenuRolePermissions`
tables, role-filtered by `GetMenuTreeQuery`, seeded by `AppMenuSeeder`. Admin pages (`Web/Components/Pages/Admin/**`) CRUD
users, tenants, modules, menu-permissions, numbering scopes.

### Tenancy (read before touching admin/tenants)
The app is **single-restaurant**. The `Tenant`, `NumberingScope`, `AppMenu*` tables and their admin pages exist and are
fully functional, but there is **no per-entity `TenantId`, no `ITenantContext`, no tenant query-filter isolation** — the
Tenants page is a manageable list, not a data-partitioning boundary. When porting more code from the reference, strip tenant
references (`ITenantContext`, `TenantId` assignments, `IgnoreQueryFilters` tenant predicates) rather than reintroducing them.

### Customer site (Next.js)
App Router + Server Components fetch the API directly via `lib/api.ts` (reads JWT from an httpOnly cookie set by
`lib/auth.ts`). Cart is **client-side localStorage** (`lib/cart.ts`); the order only hits the backend at checkout. BFF route
handlers under `app/api/**` proxy auth + order placement so the JWT stays server-side.

## Conventions
- Order numbers: `ORD-yyyyMMdd-NNNN` (date-based, `OrderNumberGenerator`).
- C#: feature-folder records-with-handlers, namespaces `BornoBit.Restaurant.{Layer}.{Feature}`.
- When porting `.razor`/`.cs` files from the reference, do byte-accurate UTF-8 namespace replacement
  (`HealthCareApp` → `BornoBit.Restaurant`) — PowerShell `Set-Content` mangles multibyte chars (em-dashes, bullets);
  use `[System.IO.File]::ReadAllText/WriteAllText` with `UTF8Encoding($false)`.
