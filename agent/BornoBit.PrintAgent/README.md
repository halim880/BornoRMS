# BornoBit Local Print Agent

Runs on the **restaurant counter PC** (the machine the thermal printer is attached to). It dials
*outbound* into the cloud staff console's SignalR print hub, so the VPS never needs to reach into the
restaurant network. When the cashier prints a receipt or fires an order, the server pushes the job down
this connection and the agent renders ESC/POS and sends it to the Windows printer.

This is the counterpart to the server's **Hub mode**
(`PrintAgent:Mode = "Hub"` in the staff console's `appsettings.json`).

## How it fits together

```
Staff console (VPS)                         Counter PC (this agent)            Thermal printer
  ReceiptPrintService ──push "Print"──▶  HubWorker ──▶ JobProcessor ──RAW──▶  USB/Network printer
  PrintAgentKitchenTicketSender          (SignalR, dials out)  (winspool.drv)
```

The agent authenticates with a shared **X-Agent-Key** that must equal the server's `PrintAgent:ApiKey`,
and connects with `?agentId=` that must equal the server's `PrintAgent:AgentId`.

## Configuration (`appsettings.json`)

| Key | Meaning |
|-----|---------|
| `HubUrl` | Full hub URL, e.g. `http://bornobit.innovatixinfosys.com/hubs/print` |
| `AgentId` | Must match server `PrintAgent:AgentId` (default `counter-1`) |
| `ApiKey` | Shared secret; must match server `PrintAgent:ApiKey` |
| `ReceiptPrinterName` | Windows printer name for receipts. Empty = system default printer |
| `KitchenPrinterName` | Windows printer name for KOTs. Empty = falls back to the receipt printer |
| `PaperWidthChars` | `48` for 80mm paper, `32` for 58mm |
| `AllowCashDrawer` | `false` to never kick the drawer from this PC |
| `FullCut` | `true` = full cut, `false` = partial cut |

Find the exact Windows printer name in **Settings ▸ Bluetooth & devices ▸ Printers & scanners**
(use the name exactly as shown, including spaces).

## Run it (quick test)

```
dotnet run --project agent/BornoBit.PrintAgent
```

You should see `Connected to hub as 'counter-1'.` here and `Print agent 'counter-1' connected` in the
VPS log. Then print a receipt from the staff console.

## Deploy as a Windows service (recommended on the counter PC)

1. Publish a self-contained build (no .NET install needed on the counter PC):
   ```
   dotnet publish agent/BornoBit.PrintAgent -c Release -r win-x64 --self-contained true ^
     -p:PublishSingleFile=true -o C:\BornoBitPrintAgent
   ```
2. Edit `C:\BornoBitPrintAgent\appsettings.json` (set the printer name; confirm HubUrl/ApiKey/AgentId).
3. Install + start the service (run the shell **as Administrator**):
   ```
   sc.exe create "BornoBitPrintAgent" binPath= "C:\BornoBitPrintAgent\BornoBit.PrintAgent.exe" start= auto
   sc.exe description "BornoBitPrintAgent" "BornoBit local thermal print agent"
   sc.exe start "BornoBitPrintAgent"
   ```
   To update later: `sc.exe stop`, replace the files, `sc.exe start`. To remove: `sc.exe delete "BornoBitPrintAgent"`.

The same exe also runs as a plain console app (just double-click) — handy for first-run testing.

## Connectivity requirements

- The agent makes an **outbound** HTTP/WebSocket connection to `HubUrl`; no inbound firewall rule needed.
- The VPS reverse proxy (nginx/IIS) **must forward WebSocket upgrades** on `/hubs/print`
  (`Connection: Upgrade` / `Upgrade: websocket`). SignalR falls back to long-polling if blocked, but
  WebSockets are strongly preferred for print latency.
- The site is served over **HTTP** today. If it later moves to HTTPS, just change `HubUrl` to `https://…`.

## Troubleshooting

| Symptom (server side) | Cause |
|-----------------------|-------|
| `Print agent 'counter-1' is not connected to the hub.` | Agent not running, wrong `ApiKey`/`AgentId`, or WebSocket blocked by the proxy |
| `Rejected print-agent connection` (VPS log) | `ApiKey` mismatch between agent and server |
| Job acknowledged but nothing prints | Wrong `ReceiptPrinterName`, printer offline/out of paper |
| `Connection refused (127.0.0.1:9123)` | Server is still in **Http** mode — set `PrintAgent:Mode = "Hub"` on the VPS |
