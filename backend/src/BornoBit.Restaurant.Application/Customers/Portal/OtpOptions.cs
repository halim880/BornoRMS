namespace BornoBit.Restaurant.Application.Customers.Portal;

public class OtpOptions
{
    public const string SectionName = "Otp";

    public int CodeLength { get; set; } = 6;
    public int TtlMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 5;
    public int RequestRateLimitPerHour { get; set; } = 5;
    public string HashPepper { get; set; } = string.Empty;
    public bool ReturnCodeInDev { get; set; } = false;
}
