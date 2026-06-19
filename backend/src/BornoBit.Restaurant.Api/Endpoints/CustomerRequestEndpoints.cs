using BornoBit.Restaurant.Api.Services;
using BornoBit.Restaurant.Application.Operations.Dashboard;
using BornoBit.Restaurant.Domain.Dining;
using BornoBit.Restaurant.Shared.Common;
using MediatR;

namespace BornoBit.Restaurant.Api.Endpoints;

/// <summary>
/// Lets the customer/QR flow raise a service request (call waiter, request bill, water, tissue) for a table.
/// Surfaces on the staff Operations Dashboard in real time.
/// </summary>
public static class CustomerRequestEndpoints
{
    public static IEndpointRouteBuilder MapCustomerRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/customer/requests")
            .RequireCors("Frontends")
            .RequireAuthorization("Customer")
            .WithTags("CustomerRequests");

        group.MapPost("", async (CreateRequestBody body, ISender sender, ILiveNotifier live, CancellationToken ct) =>
        {
            try
            {
                var id = await sender.Send(new CreateCustomerRequestCommand(body.TableId, body.Type, body.Note), ct);
                await live.NotifyAsync(LiveScopes.Requests, ct);
                return Results.Created($"/customer/requests/{id}", new { id });
            }
            catch (NotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
        });

        return app;
    }

    public record CreateRequestBody(Guid TableId, CustomerRequestType Type, string? Note = null);
}
