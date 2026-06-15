using BornoBit.Restaurant.Domain.Ordering;

namespace BornoBit.Restaurant.Application.Ordering.Printing;

/// <summary>
/// Default no-op sender. The API host has no print agent, so QR orders auto-confirmed there are left
/// <see cref="KotPrintStatus.Failed"/> and printed by the Web <c>KotPrintRetryService</c> within seconds.
/// The Web host overrides this binding with the real print-agent sender.
/// </summary>
public sealed class NullKitchenTicketSender : IKitchenTicketSender
{
    public Task<bool> SendAsync(Order order, CancellationToken cancellationToken = default) => Task.FromResult(false);
}
