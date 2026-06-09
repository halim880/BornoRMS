using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Application.Common.Sms;
using BornoBit.Restaurant.Domain.Customers;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace BornoBit.Restaurant.Application.Customers.Portal;

public record RequestOtpCommand(string Phone) : IRequest<RequestOtpResult>;

public record RequestOtpResult(bool Sent, string? DevCode);

public class RequestOtpCommandValidator : AbstractValidator<RequestOtpCommand>
{
    public RequestOtpCommandValidator()
    {
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(40);
    }
}

public class RequestOtpCommandHandler : IRequestHandler<RequestOtpCommand, RequestOtpResult>
{
    private readonly IAppDbContext _db;
    private readonly ISmsSender _sms;
    private readonly OtpOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RequestOtpCommandHandler> _logger;

    public RequestOtpCommandHandler(
        IAppDbContext db,
        ISmsSender sms,
        IOptions<OtpOptions> options,
        TimeProvider timeProvider,
        ILogger<RequestOtpCommandHandler> logger)
    {
        _db = db;
        _sms = sms;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<RequestOtpResult> Handle(RequestOtpCommand request, CancellationToken cancellationToken)
    {
        var phone = request.Phone.Trim();
        if (string.IsNullOrWhiteSpace(phone))
            return new RequestOtpResult(false, null);

        // Upsert customer by phone — self-registration on first OTP.
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Phone == phone, cancellationToken);

        if (customer is null)
        {
            customer = Customer.Create(phone);
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("OTP request: registered new customer {CustomerId} for phone", customer.Id);
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var rateWindowStart = nowUtc.AddHours(-1);
        var recentCount = await _db.CustomerOtps
            .CountAsync(o => o.CustomerId == customer.Id && o.CreatedAtUtc >= rateWindowStart, cancellationToken);

        if (recentCount >= _options.RequestRateLimitPerHour)
        {
            _logger.LogWarning("OTP rate limit hit for customer {CustomerId}", customer.Id);
            return new RequestOtpResult(false, null);
        }

        var code = GenerateCode(_options.CodeLength);
        var hash = HashCode(code, _options.HashPepper, customer.Id);

        var otp = CustomerOtp.Create(
            customer.Id,
            hash,
            nowUtc,
            TimeSpan.FromMinutes(_options.TtlMinutes),
            _options.MaxAttempts);

        _db.CustomerOtps.Add(otp);
        await _db.SaveChangesAsync(cancellationToken);

        var message = $"Your BornoBit verification code is {code}. Expires in {_options.TtlMinutes} minutes.";
        await _sms.SendAsync(customer.Phone, message, cancellationToken);

        _logger.LogInformation("OTP issued for customer {CustomerId}", customer.Id);
        return new RequestOtpResult(true, _options.ReturnCodeInDev ? code : null);
    }

    private static string GenerateCode(int length)
    {
        Span<byte> buffer = stackalloc byte[4];
        var sb = new StringBuilder(length);
        while (sb.Length < length)
        {
            RandomNumberGenerator.Fill(buffer);
            var n = BitConverter.ToUInt32(buffer);
            sb.Append((n % 10).ToString());
        }
        return sb.ToString();
    }

    private static string HashCode(string code, string pepper, Guid customerId)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(pepper));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{customerId:N}|{code}"));
        return Convert.ToHexString(bytes);
    }
}
