using BornoBit.Restaurant.Application.Common.Sms;
using Microsoft.Extensions.Logging;

namespace BornoBit.Restaurant.Infrastructure.Sms;

public class StubSmsSender : ISmsSender
{
    private readonly ILogger<StubSmsSender> _logger;

    public StubSmsSender(ILogger<StubSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string phone, string message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[StubSmsSender] To={Phone} Message={Message}", phone, message);
        return Task.CompletedTask;
    }
}
