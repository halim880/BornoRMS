namespace BornoBit.Restaurant.Application.Common.Numbering;

/// <summary>Allocates the next payroll-run number (e.g. <c>PR-yyyyMM-NNNN</c>).</summary>
public interface IPayrollNumberGenerator
{
    Task<string> NextAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}
