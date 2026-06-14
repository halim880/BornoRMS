using BornoBit.Restaurant.Application.Ordering.Commands;
using BornoBit.Restaurant.Domain.Ordering;
using BornoBit.Restaurant.Shared.Common;

namespace BornoBit.Restaurant.Application.Ordering.Payments;

/// <summary>A card/mobile authorization request handed to a provider adapter before the tender is persisted.</summary>
public record PaymentAuthorizationRequest(
    PaymentMethod Method,
    PaymentProvider? Provider,
    decimal Amount,
    string OrderNumber,
    string? Reference);

/// <summary>Provider response. <see cref="Reference"/> is the authoritative txn/auth id stored on the Payment row.</summary>
public record PaymentAuthorizationResult(bool Approved, string Reference, string? Message);

/// <summary>
/// Adapter seam for non-cash payment providers (card terminals, mobile wallets). The app never talks to a
/// real gateway here — <c>MockPaymentGateway</c> is the demo implementation. A production integration
/// implements this interface (capturing keys/secrets securely) and is swapped in via DI; cash never touches it.
/// </summary>
public interface IPaymentGateway
{
    Task<PaymentAuthorizationResult> AuthorizeAsync(PaymentAuthorizationRequest request, CancellationToken cancellationToken = default);
}

public static class PaymentGatewayExtensions
{
    /// <summary>
    /// Cash passes straight through; card/mobile tenders are authorized by the provider adapter, and the
    /// returned authoritative reference is stamped onto the tender before it is persisted. A declined
    /// authorization aborts the whole settlement (thrown as a <see cref="ConflictException"/>).
    /// </summary>
    public static async Task<PaymentEntryInput> AuthorizeIfNonCashAsync(
        this IPaymentGateway gateway, PaymentEntryInput input, string orderNumber, CancellationToken cancellationToken)
    {
        if (input.Method == PaymentMethod.Cash) return input;

        var auth = await gateway.AuthorizeAsync(
            new PaymentAuthorizationRequest(input.Method, input.Provider, input.Amount, orderNumber, input.Reference),
            cancellationToken);

        if (!auth.Approved)
            throw new ConflictException(auth.Message ?? "Card/mobile payment was declined by the provider.");

        return input with { Reference = auth.Reference };
    }
}
