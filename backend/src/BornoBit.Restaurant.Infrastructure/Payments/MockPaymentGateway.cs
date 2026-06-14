using BornoBit.Restaurant.Application.Ordering.Payments;

namespace BornoBit.Restaurant.Infrastructure.Payments;

/// <summary>
/// Demo payment-provider adapter: approves every card/mobile tender and synthesizes an authorization
/// reference when the cashier did not key one in. Stands in for a real terminal/wallet SDK so the split
/// payment flow is end-to-end testable. Replace with a real <see cref="IPaymentGateway"/> for production
/// (see README — secure key handling, capture/refund callbacks, idempotency keys).
/// </summary>
public sealed class MockPaymentGateway : IPaymentGateway
{
    public Task<PaymentAuthorizationResult> AuthorizeAsync(
        PaymentAuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        var reference = string.IsNullOrWhiteSpace(request.Reference)
            ? $"MOCK-{Tag(request)}-{Guid.NewGuid():N}"[..18].ToUpperInvariant()
            : request.Reference!.Trim();

        return Task.FromResult(new PaymentAuthorizationResult(Approved: true, reference, "Approved (mock provider)"));
    }

    private static string Tag(PaymentAuthorizationRequest r) =>
        r.Provider is { } p && p != Domain.Ordering.PaymentProvider.None ? p.ToString() : r.Method.ToString();
}
