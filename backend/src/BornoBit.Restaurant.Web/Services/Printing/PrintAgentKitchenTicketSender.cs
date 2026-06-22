using System.Net.Http.Json;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Ordering.Printing;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Reporting.Models;
using BornoBit.Restaurant.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BornoBit.Restaurant.Web.Services.Printing;

/// <summary>
/// Dispatches the kitchen order ticket to the local print agent over the same HTTP/SignalR transport as
/// receipts (<see cref="ReceiptPrintService"/>), but with a <see cref="KitchenTicketPayload"/> instead of a
/// receipt. Returns true only when the agent acknowledges the job; <see cref="OrderKotSync"/> turns a false
/// return (or a throw) into a Failed status the retry worker re-attempts.
/// </summary>
public sealed class PrintAgentKitchenTicketSender(
    IAppDbContext db,
    IHttpClientFactory httpClientFactory,
    IHubContext<PrintHub> printHub,
    IOptions<PrintAgentOptions> options,
    IOptions<ReceiptBranding> branding) : IKitchenTicketSender
{
    public async Task<bool> SendAsync(Order order, CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        if (opts.IsOff || !opts.AutoPrintKot)
            return false;

        var tableNumber = order.RestaurantTableId is { } tid
            ? await db.RestaurantTables.Where(t => t.Id == tid).Select(t => t.TableNumber).FirstOrDefaultAsync(cancellationToken)
            : null;
        var customerName = await db.Customers.Where(c => c.Id == order.CustomerId).Select(c => c.FullName).FirstOrDefaultAsync(cancellationToken);

        // A line's kitchen is its station's kitchen; station-less lines (and stations with no kitchen)
        // fall back to the default kitchen. We print one ticket per kitchen, each to its own printer.
        var stationKitchen = await db.KitchenStations
            .Select(s => new { s.Id, s.KitchenId })
            .ToListAsync(cancellationToken);
        var stationToKitchen = stationKitchen.ToDictionary(s => s.Id, s => s.KitchenId);

        var kitchens = await db.Kitchens
            .Select(k => new { k.Id, k.Name, k.PrinterName, k.IsDefault })
            .ToListAsync(cancellationToken);
        var kitchenById = kitchens.ToDictionary(k => k.Id);
        Guid? defaultKitchenId = kitchens.FirstOrDefault(k => k.IsDefault)?.Id;

        Guid? ResolveKitchen(Guid? stationId)
        {
            if (stationId is { } sid && stationToKitchen.TryGetValue(sid, out var kid) && kid is { } resolved)
                return resolved;
            return defaultKitchenId;
        }

        // Group the order's lines by resolved kitchen, preserving a stable order for printing.
        var groups = order.Lines
            .GroupBy(l => ResolveKitchen(l.StationId))
            .OrderBy(g => g.Key is { } k && kitchenById.TryGetValue(k, out var info) ? info.Name : "~")
            .ToList();

        var allAcknowledged = true;
        foreach (var group in groups)
        {
            string? kitchenName = group.Key is { } kid && kitchenById.TryGetValue(kid, out var info) ? info.Name : null;
            string? kitchenPrinter = group.Key is { } kid2 && kitchenById.TryGetValue(kid2, out var info2) ? info2.PrinterName : null;

            var request = BuildRequest(order, tableNumber, customerName, opts, group.ToList(), kitchenName, kitchenPrinter);

            var response = opts.IsHub
                ? await SendViaHubAsync(request, opts, cancellationToken)
                : await SendViaHttpAsync(request, cancellationToken);

            if (response is null) allAcknowledged = false;
        }

        // No lines ⇒ nothing to print; treat as acknowledged so the order isn't stuck Pending.
        return allAcknowledged;
    }

    private PrintJobRequest BuildRequest(
        Order order, string? tableNumber, string? customerName, PrintAgentOptions opts,
        IReadOnlyList<OrderLine> lines, string? kitchenName, string? kitchenPrinter)
    {
        var b = branding.Value;
        var label = string.IsNullOrWhiteSpace(kitchenName)
            ? $"KOT · {order.OrderNumber}"
            : $"KOT · {order.OrderNumber} · {kitchenName}";
        return new PrintJobRequest
        {
            JobId = Guid.NewGuid(),
            OrderId = order.Id,
            PrinterName = kitchenPrinter ?? opts.KitchenPrinterName ?? opts.PrinterName,
            KitchenTicket = new KitchenTicketPayload
            {
                RestaurantName = b.Name,
                TimeZoneId = b.TimeZoneId,
                OrderNumber = order.OrderNumber,
                TicketLabel = label,
                OrderType = order.OrderType.ToString(),
                TableNumber = tableNumber,
                CustomerName = customerName,
                OrderedAtUtc = order.OrderedAtUtc,
                IsPriority = order.IsPriority,
                KitchenNotes = order.KitchenNotes,
                Notes = order.Notes,
                Lines = lines.Select(l => new KitchenTicketLinePayload
                {
                    Name = l.Name,
                    Quantity = l.Quantity,
                    Notes = l.Notes,
                    StationName = l.StationName,
                    Modifiers = l.Modifiers.Select(m => m.OptionName).ToList()
                }).ToList()
            }
        };
    }

    private async Task<PrintJobResponse?> SendViaHttpAsync(PrintJobRequest request, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("PrintAgent");
        var response = await client.PostAsJsonAsync("/print", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PrintJobResponse>(ct);
    }

    private async Task<PrintJobResponse?> SendViaHubAsync(PrintJobRequest request, PrintAgentOptions opts, CancellationToken ct)
    {
        var connectionId = PrintHub.GetConnectionId(opts.AgentId)
            ?? throw new InvalidOperationException($"Print agent '{opts.AgentId}' is not connected to the hub.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, opts.TimeoutSeconds)));
        return await printHub.Clients.Client(connectionId)
            .InvokeAsync<PrintJobResponse>("Print", request, timeout.Token);
    }
}
