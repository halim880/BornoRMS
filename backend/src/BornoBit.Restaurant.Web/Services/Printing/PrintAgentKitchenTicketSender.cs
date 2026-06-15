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

        var request = BuildRequest(order, tableNumber, customerName, opts);

        var response = opts.IsHub
            ? await SendViaHubAsync(request, opts, cancellationToken)
            : await SendViaHttpAsync(request, cancellationToken);

        return response is not null;
    }

    private PrintJobRequest BuildRequest(Order order, string? tableNumber, string? customerName, PrintAgentOptions opts)
    {
        var b = branding.Value;
        return new PrintJobRequest
        {
            JobId = Guid.NewGuid(),
            OrderId = order.Id,
            PrinterName = opts.KitchenPrinterName ?? opts.PrinterName,
            KitchenTicket = new KitchenTicketPayload
            {
                RestaurantName = b.Name,
                TimeZoneId = b.TimeZoneId,
                OrderNumber = order.OrderNumber,
                TicketLabel = $"KOT · {order.OrderNumber}",
                OrderType = order.OrderType.ToString(),
                TableNumber = tableNumber,
                CustomerName = customerName,
                OrderedAtUtc = order.OrderedAtUtc,
                IsPriority = order.IsPriority,
                KitchenNotes = order.KitchenNotes,
                Notes = order.Notes,
                Lines = order.Lines.Select(l => new KitchenTicketLinePayload
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
