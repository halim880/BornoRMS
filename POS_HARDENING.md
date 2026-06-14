# POS Production Hardening

This branch hardens the POS for a live restaurant: real-time sync, split/partial payments,
a payment-provider seam, inventory enforcement, table locking, manager-approved voids/refunds,
cashier drawers, and domain unit tests. Money math is decimal throughout and rounded to 2 dp.

## Run locally

From `backend/`:

```bash
dotnet build BornoBit.Restaurant.slnx
dotnet run --project src/BornoBit.Restaurant.Web      # staff console + POS on :5002 (auto-migrates + seeds)
```

The Web app auto-applies migrations on startup, including `AddTableHold`. Sign in with the seeded
super-admin `admin@bornobit.local` / `ChangeMe!2026`. POS is at `/pos`.

> Run the staff console in **Development** (plain `dotnet run`) or component styles 404 — see CLAUDE.md.

## Run the tests

```bash
dotnet test tests/BornoBit.Restaurant.Tests.Unit
```

19 domain tests cover: split & partial payments, cash overpay → change, non-cash cannot exceed the
balance, void reversal, full refund, decimal discount rounding, single-tender change rules, and the
table edit-hold race (acquire / conflict / expiry / refresh / release).

## Multi-terminal test guide

Open two browser sessions (e.g. a normal window + an incognito window), sign in to `/pos` in both —
each acts as a separate POS terminal. The header shows a green **Live** dot when the real-time channel
is connected.

1. **Real-time sync** — in terminal A start a new order and add items. Within ~2s terminal B's order
   chips, totals and table occupancy update with no manual refresh.
2. **Table lock race** — in terminal A open *New order → Dine In* and select table T1 (this takes a
   3-minute hold). In terminal B try the same table: it is rejected with "being used by …". Close/cancel
   A's dialog (or wait out the hold) and B can take it.
3. **Split / partial payment** — Checkout an order, add a 120 cash tender then an 80 bKash tender; the
   *Remaining* line drops to 0 and it settles. Or take a partial tender and confirm the order stays open
   on the new balance.
4. **Manager approval** — as a non-manager cashier, open an order's **Payments** dialog and Void/Refund a
   tender: it requires a manager username + password (validated server-side, no sign-in) and the approver
   is recorded in the financial audit log. A manager on the till skips the prompt.
5. **Inventory** — mark a DirectStock product's stock to 0; in POS it greys out, shows an **Out** badge and
   can't be added. Low stock shows a **Low N** badge.
6. **Cashier drawer** — the wallet button on the POS header opens/closes the cash drawer; the close screen
   summarizes takings by method and computes the variance. Import the day's takings from
   *Accounts → Transactions* (`ImportCashCounterCommand`), which ties to the captured drawer session.

## Design notes & tradeoffs

- **Real-time** reuses the existing `DashboardHub` content-free "changed" tick + the 5s DB-poll bridge,
  and adds a direct `IDashboardNotifier` push after each POS mutation for sub-2s sync. No POS-specific hub.
- **Concurrency**: settlement is guarded by `Order.RowVersion` (optimistic) — a second terminal settling the
  same order gets a `ConflictException` ("reload and try again"). RowVersion concurrency is an
  EF/integration concern and is exercised by the handlers, not the domain unit tests.
- **Payment provider** is a mock (`MockPaymentGateway`) behind `IPaymentGateway`. Production steps: implement
  the interface against a real terminal/wallet SDK, handle secure key storage (never in source/config),
  add capture/refund webhooks and idempotency keys, and swap it in via DI. Cash never touches the gateway.
- **Table hold** is a conservative 3-minute DB-backed timestamp (configurable in `TableHoldOptions`), released
  on close/cancel/type-change or once the order owns the table; expiry is lazy (checked on acquire).
- **Manager approval** is the instant-override model (validate a manager credential at the point of action),
  not an async approval queue. Approver + reason land in `FinancialAuditLog`.
- **Migration note**: `AddTableHold` was hand-authored because both host apps were running and locked the
  build output; its Up/Down and the model snapshot are authoritative. Regenerate with `dotnet ef migrations
  add` (apps stopped) to repopulate the designer's target model if desired.

## Open questions

- Real card/mobile terminal integration (secure key handling, settlement reconciliation) is out of scope —
  only the mock adapter ships here.
- Integration tests for SignalR and RowVersion conflicts would need a test host + LocalDB (or an EF provider
  abstraction); the current suite is domain-level only.
