using BornoBit.Restaurant.Application.Customers.Portal;
using BornoBit.Restaurant.Shared.Common;
using MediatR;

namespace BornoBit.Restaurant.Api.Endpoints;

public static class CustomerAuthEndpoints
{
    public static IEndpointRouteBuilder MapCustomerAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth")
            .RequireCors("Frontends")
            .WithTags("CustomerAuth");

        group.MapPost("/request-otp", async (RequestOtpRequest body, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RequestOtpCommand(body.Phone), ct);
            return Results.Accepted(value: new
            {
                message = "If the phone is valid, an OTP has been sent.",
                devCode = result.DevCode
            });
        });

        group.MapPost("/verify-otp", async (VerifyOtpRequest body, ISender sender, CancellationToken ct) =>
        {
            try
            {
                var result = await sender.Send(new VerifyOtpCommand(body.Phone, body.Code), ct);
                return Results.Ok(result);
            }
            catch (NotFoundException ex)
            {
                return Results.Json(new { message = ex.Message }, statusCode: StatusCodes.Status401Unauthorized);
            }
        });

        return app;
    }

    public record RequestOtpRequest(string Phone);
    public record VerifyOtpRequest(string Phone, string Code);
}
