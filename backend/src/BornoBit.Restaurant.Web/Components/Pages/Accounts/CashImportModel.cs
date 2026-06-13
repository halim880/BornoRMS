namespace BornoBit.Restaurant.Web.Components.Pages.Accounts;

/// <summary>Carrier passed into <c>ImportCashCounterDialog</c> and returned with the import result.</summary>
public class CashImportModel
{
    public int ImportedCount { get; set; }
    public decimal ImportedTotal { get; set; }
    public List<string> SkippedMethods { get; set; } = new();
}
