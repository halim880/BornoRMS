namespace BornoBit.Restaurant.Application.Common.Numbering;

/// <summary>Allocates the next cash-drawer shift number (e.g. <c>DRW-yyyyMMdd-NNNN</c>).</summary>
public interface IDrawerNumberGenerator
{
    Task<string> NextAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}
