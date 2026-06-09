namespace BornoBit.Restaurant.Application.Common.Sms;

public interface ISmsSender
{
    Task SendAsync(string phone, string message, CancellationToken cancellationToken = default);
}
