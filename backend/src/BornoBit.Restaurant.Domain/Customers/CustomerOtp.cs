using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Customers;

public class CustomerOtp : AuditableEntity
{
    public Guid CustomerId { get; private set; }
    public string CodeHash { get; private set; } = default!;
    public DateTime ExpiresAtUtc { get; private set; }
    public int AttemptsRemaining { get; private set; }
    public DateTime? ConsumedAtUtc { get; private set; }

    private CustomerOtp() { }

    public static CustomerOtp Create(
        Guid customerId,
        string codeHash,
        DateTime nowUtc,
        TimeSpan ttl,
        int maxAttempts)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("CustomerId required.", nameof(customerId));
        if (string.IsNullOrWhiteSpace(codeHash)) throw new ArgumentException("CodeHash required.", nameof(codeHash));
        if (ttl <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(ttl));
        if (maxAttempts <= 0) throw new ArgumentOutOfRangeException(nameof(maxAttempts));

        return new CustomerOtp
        {
            CustomerId = customerId,
            CodeHash = codeHash,
            ExpiresAtUtc = nowUtc.Add(ttl),
            AttemptsRemaining = maxAttempts
        };
    }

    public bool IsActive(DateTime nowUtc) =>
        ConsumedAtUtc is null && AttemptsRemaining > 0 && ExpiresAtUtc > nowUtc;

    public void DecrementAttempt() => AttemptsRemaining = Math.Max(0, AttemptsRemaining - 1);

    public void Consume(DateTime nowUtc) => ConsumedAtUtc = nowUtc;
}
