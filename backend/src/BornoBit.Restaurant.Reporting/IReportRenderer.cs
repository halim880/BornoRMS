using BornoBit.Restaurant.Reporting.Models;

namespace BornoBit.Restaurant.Reporting;

public interface IReportRenderer
{
    Task<byte[]> RenderOrderReceiptAsync(OrderReceiptReportData data, CancellationToken cancellationToken = default);
}
