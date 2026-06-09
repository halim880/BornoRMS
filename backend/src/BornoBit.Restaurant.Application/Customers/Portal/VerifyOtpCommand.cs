using BornoBit.Restaurant.Application.Common.Identity;
using BornoBit.Restaurant.Application.Common.Persistence;
using BornoBit.Restaurant.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace BornoBit.Restaurant.Application.Customers.Portal;

public record VerifyOtpCommand(string Phone, string Code) : IRequest<CustomerLoginResult>;

public class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MinimumLength(4).MaximumLength(10);
    }
}

public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, CustomerLoginResult>
{
    private readonly IAppDbContext _db;
    private readonly ICustomerTokenService _tokenService;
    private readonly OtpOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<VerifyOtpCommandHandler> _logger;

    public VerifyOtpCommandHandler(
        IAppDbContext db,
        ICustomerTokenService tokenService,
        IOptions<OtpOptions> options,
        TimeProvider timeProvider,
        ILogger<VerifyOtpCommandHandler> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<CustomerLoginResult> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var phone = request.Phone.Trim();
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Phone == phone && c.IsActive, cancellationToken);

        if (customer is null) throw new NotFoundException("Invalid or expired code.");

        var otp = await _db.CustomerOtps
            .Where(o => o.CustomerId == customer.Id && o.ConsumedAtUtc == null)
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null) throw new NotFoundException("Invalid or expired code.");

        if (!otp.IsActive(nowUtc))
        {
            _logger.LogInformation("OTP inactive for customer {CustomerId}", customer.Id);
            throw new NotFoundException("Invalid or expired code.");
        }

        var providedHash = HashCode(request.Code.Trim(), _options.HashPepper, customer.Id);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(providedHash),
                Encoding.UTF8.GetBytes(otp.CodeHash)))
        {
            otp.DecrementAttempt();
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("OTP mismatch for customer {CustomerId}, attempts remaining {N}", customer.Id, otp.AttemptsRemaining);
            throw new NotFoundException("Invalid or expired code.");
        }

        otp.Consume(nowUtc);
        await _db.SaveChangesAsync(cancellationToken);

        var token = _tokenService.IssueAccessToken(customer.Id, customer.Phone, customer.FullName);
        var dto = new CustomerDto(customer.Id, customer.Phone, customer.FullName);

        return new CustomerLoginResult(token.AccessToken, token.ExpiresAtUtc, dto);
    }

    private static string HashCode(string code, string pepper, Guid customerId)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(pepper));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{customerId:N}|{code}"));
        return Convert.ToHexString(bytes);
    }
}
