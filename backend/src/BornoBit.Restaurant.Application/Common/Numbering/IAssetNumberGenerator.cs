namespace BornoBit.Restaurant.Application.Common.Numbering;

/// <summary>Allocates the next fixed-asset number (e.g. <c>FA-yyyyMMdd-NNNN</c>).</summary>
public interface IAssetNumberGenerator
{
    Task<string> NextAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}
