using System.Net.Http.Json;
using BornoBit.Restaurant.Application.Ordering.Queries;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Reporting.Models;
using BornoBit.Restaurant.Web.Hubs;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace BornoBit.Restaurant.Web.Services.Printing;

public sealed class ReceiptPrintService(
    ISender sender,
    IHttpClientFactory httpClientFactory,
    IHubContext<PrintHub> printHub,
    IOptions<PrintAgentOptions> options,
    IOptions<ReceiptBranding> branding,
    IServiceProvider serviceProvider,
    ILogger<ReceiptPrintService> logger) : IReceiptPrintService
{
    public async Task<PrintResult> PrintReceiptAsync(Guid orderId, bool isReprint, CancellationToken ct = default)
    {
        var opts = options.Value;
        try
        {
            if (opts.IsOff)
                return new PrintResult(false, "Receipt printing is disabled (PrintAgent:Mode = Off).");

            var order = await sender.Send(new GetOrderQuery(orderId), ct);
            var request = BuildRequest(order, isReprint, await GetCashierNameAsync(), opts);

            var response = opts.IsHub
                ? await SendViaHubAsync(request, opts, ct)
                : await SendViaHttpAsync(request, ct);

            if (response is null)
                return new PrintResult(false, "Print agent did not acknowledge the job.");

            var verb = response.Deduplicated ? "was already sent to the printer"
                : isReprint ? "reprint sent to printer"
                : "sent to printer";
            return new PrintResult(true, $"Receipt {order.OrderNumber}: {verb}.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not deliver order {OrderId} to the print agent ({Mode})", orderId, opts.Mode);
            return new PrintResult(false, $"Receipt not printed — print agent unreachable. ({ex.Message})");
        }
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

    private PrintJobRequest BuildRequest(OrderDetailDto order, bool isReprint, string? cashierName, PrintAgentOptions opts)
    {
        var b = branding.Value;
        return new PrintJobRequest
        {
            JobId = Guid.NewGuid(),
            OrderId = order.Id,
            IsReprint = isReprint,
            PrinterName = opts.PrinterName,
            OpenCashDrawer = opts.OpenCashDrawerOnCashPayment &&
                             !isReprint &&
                             order.PaymentMethod == PaymentMethod.Cash,
            Receipt = new ReceiptPayload
            {
                RestaurantName = b.Name,
                Address = b.Address,
                Phone = b.Phone,
                VatRegistrationNo = b.VatRegistrationNo,
                Website = b.Website,
                ThankYouLine = b.ThankYouLine,
                VisitAgainLine = b.VisitAgainLine,
                TimeZoneId = b.TimeZoneId,
                OrderNumber = order.OrderNumber,
                OrderType = order.OrderType.ToString(),
                TableNumber = order.TableNumber,
                CustomerName = order.CustomerName,
                CustomerPhone = order.CustomerPhone,
                CashierName = cashierName,
                Notes = order.Notes,
                OrderedAtUtc = order.OrderedAtUtc,
                PaidAtUtc = order.PaidAtUtc,
                Currency = order.Currency,
                Lines = order.Lines.Select(l => new ReceiptLine
                {
                    Code = l.Code,
                    Name = l.Name,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    LineTotal = l.LineTotal
                }).ToList(),
                Subtotal = order.Subtotal,
                DiscountAmount = order.DiscountAmount,
                RoundingAdjustment = order.RoundingAdjustment,
                Total = order.Total,
                IsPaid = order.IsPaid,
                PaymentMethod = order.PaymentMethod?.ToString(),
                AmountTendered = order.AmountTendered,
                ChangeGiven = order.ChangeGiven
            }
        };
    }

    private async Task<string?> GetCashierNameAsync()
    {
        // Resolved lazily: available inside a Blazor circuit, absent elsewhere.
        try
        {
            if (serviceProvider.GetService<AuthenticationStateProvider>() is not { } provider)
                return null;
            var state = await provider.GetAuthenticationStateAsync();
            return state.User.Identity?.Name;
        }
        catch
        {
            return null;
        }
    }
}
