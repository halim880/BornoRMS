# BornoBit Restaurant

Restaurant ordering + management system. Mirrors the `HealthCareApp` architecture:
**.NET 10 Clean Architecture + CQRS** backend, **Next.js 15** frontends, **SQL Server (LocalDB)**, JWT auth.

Single-restaurant (no multi-tenancy). Vertical slice implemented: **browse menu → place order**, plus staff order list.

## Layout

```
backend/        .NET 10 solution (7 projects):
                  Api            REST API (port 5000) — customer + staff JSON endpoints
                  Web            Blazor Server staff console (port 5002) — cookie login, orders, receipts
                  Application    CQRS handlers (MediatR), validators, DTOs
                  Domain         entities, enums, roles
                  Infrastructure EF Core, Identity, seeders, services
                  Shared         common types (PagedResult, exceptions, ICurrentUser)
                  Reporting      QuestPDF order-receipt PDF generation
customer-web/   Next.js customer site — phone OTP login, menu, cart, checkout   (port 3000)
```

The staff/admin console is the **Blazor `Web` project** (matches the reference architecture). It talks to the
database directly via MediatR — it does not go through the REST API.

## Prerequisites

- .NET 10 SDK
- Node.js 20+
- SQL Server LocalDB (`MSSQLLocalDB`)

## Run

**1. Backend** (creates DB + seeds menu/tables/roles/admin on first run):

```
cd backend/src/BornoBit.Restaurant.Api
dotnet run --urls http://localhost:5000
```

Swagger at http://localhost:5000/swagger.

**2. Customer site:**

```
cd customer-web
npm install
npm run dev        # http://localhost:3000
```

**3. Staff console (Blazor):**

```
cd backend/src/BornoBit.Restaurant.Web
dotnet run --urls http://localhost:5002     # http://localhost:5002
```

## Try it

- **Customer**: open http://localhost:3000 → add items to cart → Login (any phone, e.g. `01700000000`).
  The OTP is returned in dev (`Otp:ReturnCodeInDev=true`) and shown on the verify screen — also logged to the API console.
  Checkout places the order; it appears under *My Orders*.
- **Staff**: open http://localhost:5002 → login with the seeded super-admin:
  - email `admin@bornobit.local`
  - password `ChangeMe!2026`
  - New customer orders appear under *Incoming Orders*; click **Receipt** to download the PDF.

## Staff console (Blazor Server)

The `Web` project uses an imported design system (the **Borno UI** component library, `Bo*` prefix + design-token theme) ported
from the reference app. It talks to the database directly via MediatR (not through the REST API).

- **DB-driven navigation**: the left sidebar is built from the `AppMenus` / `AppMenuRolePermissions` tables,
  filtered by the signed-in user's roles (`GetMenuTreeQuery`). Seeded on first run by `AppMenuSeeder`.
- **Administration** (Admin/SuperAdmin only):
  - **Users & Roles** (`/admin/users`) — create/edit users, assign roles, reset password, activate/deactivate.
  - **Menu Permissions** (`/admin/menu-permissions`) — grant/revoke each menu per role; drives the nav.
  - **Numbering Scopes** (`/admin/numbering-scopes`) — CRUD document-numbering config rows.
  - **Tenants** (`/admin/tenants`, SuperAdmin) — CRUD tenant rows.
  - **Modules** (`/admin/modules`, SuperAdmin) — CRUD the root nav modules.
- **Settings** (`/settings/app`) — theme picker; changes `--bo-primary` live (persisted in browser localStorage).

> **Note on tenancy:** the Tenants/Numbering/Modules admin pages are present and functional, but the app is
> **single-restaurant** — there is no per-record tenant isolation. The `Tenants` table is a manageable list,
> not a data-partitioning boundary. (Re-introducing real multi-tenancy would mean adding `TenantId` + query
> filters across every entity, which was deliberately not done.)

## Key API endpoints

| Method | Path | Auth | Purpose |
|---|---|---|---|
| GET  | `/menu` | anon | Categories + available items |
| POST | `/auth/request-otp` | anon | Send phone OTP (returns dev code) |
| POST | `/auth/verify-otp` | anon | Verify code → customer JWT |
| POST | `/orders` | customer | Place an order |
| GET  | `/orders/mine` | customer | Customer's own orders |
| POST | `/staff/auth/login` | anon | Staff email/password → staff JWT |
| GET  | `/admin/orders` | staff | All orders (paged) |
| GET  | `/orders/{id}/receipt.pdf` | customer | Order receipt PDF |
| GET  | `/admin/orders/{id}/receipt.pdf` | staff | Order receipt PDF |

## Roles

`SuperAdmin, Admin, Manager, Waiter, Chef, Cashier`. The waiter module is a role-gated section of `admin-web`
(not a separate app); the order list is the first slice of it.

## MCP server (database access for AI tools)

`.mcp.json` at the repo root registers a project-scoped **SQL MCP server** (`bornobit-sql`) that lets an
MCP client (e.g. Claude Code) query the LocalDB database read-only. It's a small .NET stdio server under
[tools/BornoBit.Restaurant.Mcp/](tools/BornoBit.Restaurant.Mcp/) — see its README for tools and rationale.

Build it once so the configured DLL exists:

```
dotnet build tools/BornoBit.Restaurant.Mcp
```

Then in Claude Code it appears automatically (`/mcp` to approve/inspect). Tools: `list_tables`,
`describe_table`, `run_query` (read-only SELECT).

## Database / migrations

Connection (LocalDB) is in `backend/src/BornoBit.Restaurant.Api/appsettings.json`. The app applies migrations on
startup. To manage manually:

```
cd backend
dotnet ef migrations add <Name> -p src/BornoBit.Restaurant.Infrastructure -s src/BornoBit.Restaurant.Api
dotnet ef database update      -p src/BornoBit.Restaurant.Infrastructure -s src/BornoBit.Restaurant.Api
```

## Notes

- `SigningKey` / `HashPepper` in `appsettings.json` are dev values — replace for any real deployment.
- Customers self-register by phone on first OTP. Cart is client-side (localStorage); only checkout hits the backend.
- Order numbering is date-based (`ORD-yyyyMMdd-NNNN`).
