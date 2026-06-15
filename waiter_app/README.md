# BornoBit Waiter — Flutter app

Mobile waiter console. Full parity with the Blazor `WaiterOrders.razor` page: floor map +
table actions, take-order POS, ready-to-serve, customer requests, view bill, request payment,
KOT/bill PDF. Talks to the **new `/waiter/*` REST surface** on the API (`Api/Endpoints/WaiterEndpoints.cs`).

## Architecture
- **State:** Riverpod. Live data via **polling** (~5s, `AppConfig.pollInterval`) — no SignalR.
  `consoleProvider` polls the aggregate `/waiter/console` (dashboard + floor + ready + requests in one call).
- **Auth:** staff JWT from `POST /staff/auth/login`, stored in `flutter_secure_storage`. A 401 clears
  the token and kicks back to login. Token lives 30 min and there is **no refresh endpoint yet** — the
  app re-logs-in on expiry. (Adding `POST /staff/auth/refresh` to the API is the recommended follow-up.)
- **PDFs:** downloaded through the authenticated Dio client (the bearer header can't ride on a plain
  `url_launcher` URL), written to a temp file, opened with `open_filex`.
- **Money/ids:** decimals are display-only (server returns authoritative totals); Guids stay Strings;
  enums are parsed from their C# names with an `unknown` fallback.

## Run
The API must be running and **reachable from the phone** — `localhost` resolves to the phone itself.
Use the dev machine's LAN IP and open the port in the firewall.

```
# Android emulator (host = 10.0.2.2, the default):
flutter run

# Real device / other host:
flutter run --dart-define=API_BASE_URL=http://<DEV-MACHINE-LAN-IP>:5000

# Windows desktop:
flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5000
```

Sign in with a staff account that has the **Waiter** (or Manager/Admin) role, e.g. the seeded
super-admin `admin@bornobit.local` / `ChangeMe!2026`.

Android cleartext HTTP to a LAN IP is enabled (`usesCleartextTraffic`); for production use HTTPS.

## Layout
```
lib/core/        config · api (Dio client + WaiterApi) · auth · models (DTOs+enums) · providers · widgets
lib/features/    auth · shell · floor (+ action sheet, dialogs) · take_order (+ cart) · ready · requests
```
