using BornoBit.Restaurant.Application.Ordering.Queries;
using BornoBit.Restaurant.Reporting;
using BornoBit.Restaurant.Reporting.Models;
using BornoBit.Restaurant.Shared.Common;
using MediatR;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BornoBit.Restaurant.Api.Endpoints;

public static class ReceiptEndpoints
{
    private const string RestaurantName = "BornoBit Restaurant";

    public static IEndpointRouteBuilder MapReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        // Customer downloads their own receipt.
        app.MapGet("/orders/{id:guid}/receipt.pdf", async (
            Guid id, ClaimsPrincipal user, ISender sender, IReportRenderer renderer, CancellationToken ct) =>
        {
            var raw = user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(raw, out var customerId)) return Results.Unauthorized();

            try
            {
                var order = await sender.Send(new GetOrderQuery(id), ct);
                if (order.CustomerId != customerId) return Results.Forbid();
                var pdf = await renderer.RenderOrderReceiptAsync(ToReceipt(order), ct);
                return Results.File(pdf, "application/pdf", $"{order.OrderNumber}.pdf");
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .RequireCors("Frontends")
        .RequireAuthorization("Customer")
        .WithTags("Receipts");

        // Staff downloads any receipt.
        app.MapGet("/admin/orders/{id:guid}/receipt.pdf", async (
            Guid id, ISender sender, IReportRenderer renderer, CancellationToken ct) =>
        {
            try
            {
                var order = await sender.Send(new GetOrderQuery(id), ct);
                var pdf = await renderer.RenderOrderReceiptAsync(ToReceipt(order), ct);
                return Results.File(pdf, "application/pdf", $"{order.OrderNumber}.pdf");
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        })
        .RequireCors("Frontends")
        .RequireAuthorization("Staff")
        .WithTags("Receipts");

        return app;
    }

    private static OrderReceiptReportData ToReceipt(OrderDetailDto order) => new(
        RestaurantName: RestaurantName,
        OrderNumber: order.OrderNumber,
        OrderedAtUtc: order.OrderedAtUtc,
        OrderType: order.OrderType.ToString(),
        Status: order.Status.ToString(),
        TableNumber: order.TableNumber,
        CustomerName: order.CustomerName,
        CustomerPhone: order.CustomerPhone,
        Currency: order.Currency,
        Subtotal: order.Subtotal,
        DiscountAmount: order.DiscountAmount,
        Total: order.Total,
        IsPaid: order.IsPaid,
        PaymentMethod: order.PaymentMethod?.ToString(),
        AmountTendered: order.AmountTendered,
        ChangeGiven: order.ChangeGiven,
        Notes: order.Notes,
        GeneratedAtUtc: DateTime.UtcNow,
        Lines: order.Lines.Select(l => new OrderReceiptLine(l.Code, l.Name, l.Quantity, l.UnitPrice, l.LineTotal)).ToList()
    );
}
