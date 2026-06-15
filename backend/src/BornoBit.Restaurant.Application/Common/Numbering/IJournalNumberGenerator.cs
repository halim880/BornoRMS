namespace BornoBit.Restaurant.Application.Common.Numbering;

/// <summary>Allocates the next journal-voucher number (e.g. <c>JV-yyyyMMdd-NNNN</c>).</summary>
public interface IJournalNumberGenerator
{
    Task<string> NextAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}
