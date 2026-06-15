namespace BornoBit.Restaurant.Web.Components.Pages.Accounts.Gl;

/// <summary>Edit model for the manual journal-entry dialog.</summary>
public class JournalEntryFormModel
{
    public DateTime EntryDate { get; set; } = DateTime.UtcNow.Date;
    public string? Reference { get; set; }
    public string? Narration { get; set; }
    public List<JournalLineRow> Lines { get; set; } = new() { new(), new() };

    public bool Saved { get; set; }

    public decimal TotalDebit => Lines.Sum(l => l.Debit);
    public decimal TotalCredit => Lines.Sum(l => l.Credit);
    public bool IsBalanced => TotalDebit == TotalCredit && TotalDebit > 0m;
}

public class JournalLineRow
{
    public Guid? AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string? Narration { get; set; }
}
