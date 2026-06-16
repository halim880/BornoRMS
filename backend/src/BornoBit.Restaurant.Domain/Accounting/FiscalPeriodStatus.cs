namespace BornoBit.Restaurant.Domain.Accounting;

/// <summary>Whether a fiscal (calendar month) period still accepts postings.</summary>
public enum FiscalPeriodStatus
{
    Open = 1,
    Closed = 2
}
