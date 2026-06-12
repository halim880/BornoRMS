namespace BornoBit.Restaurant.Application.Common.Numbering;

/// <summary>Allocates the next finance transaction number (e.g. <c>TXN-yyyyMMdd-NNNN</c>).</summary>
public interface ITransactionNumberGenerator
{
    Task<string> NextAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}
