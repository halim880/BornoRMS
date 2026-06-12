namespace BornoBit.Restaurant.Web.Services.Printing;

public sealed record PrintResult(bool Success, string Message);

public interface IReceiptPrintService
{
    /// <summary>
    /// Sends an order receipt to the local print agent. Never throws — payment and UI
    /// flows must not fail because a printer is down; failures come back as PrintResult.
    /// </summary>
    Task<PrintResult> PrintReceiptAsync(Guid orderId, bool isReprint, CancellationToken ct = default);
}
