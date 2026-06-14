namespace BornoBit.Restaurant.Application.Common.Numbering;

public interface ISessionNumberGenerator
{
    Task<string> NextAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
}
