namespace BornoBit.Restaurant.Application.Common.Numbering;

public interface IOrderNumberGenerator
{
    Task<string> NextAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}
