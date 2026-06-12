namespace BornoBit.Restaurant.Reporting.Models;

/// <summary>
/// Restaurant identity printed on receipts. Bound from the "Receipt" configuration
/// section by each host so nothing on the receipt is hardcoded.
/// </summary>
public sealed class ReceiptBranding
{
    public const string SectionName = "Receipt";

    public string Name { get; set; } = "Restaurant";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? VatRegistrationNo { get; set; }
    public string? Website { get; set; }
    public string ThankYouLine { get; set; } = "Thank You For Dining With Us";
    public string VisitAgainLine { get; set; } = "Please Visit Again";

    /// <summary>IANA or Windows time-zone id used to print local times (e.g. "Asia/Dhaka").</summary>
    public string? TimeZoneId { get; set; } = "Asia/Dhaka";
}
