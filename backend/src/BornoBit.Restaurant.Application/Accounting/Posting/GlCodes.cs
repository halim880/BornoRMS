namespace BornoBit.Restaurant.Application.Accounting.Posting;

/// <summary>
/// Well-known Chart-of-Accounts codes wired by the accrual/posting features. These leaves are all
/// seeded by <c>GeneralLedgerSeeder</c> and are treated as system-protected: renaming or deactivating
/// them breaks the postings that resolve accounts by code through <see cref="IGeneralLedgerService"/>.
/// </summary>
public static class GlCodes
{
    // Assets
    public const string AccountsReceivable = "1140";
    public const string AccumulatedDepreciation = "1390"; // contra-asset, added to the seeder template

    // Liabilities
    public const string AccountsPayable = "2100";
    public const string VatPayable = "2200";
    public const string EmployeePayable = "2300";
    public const string TaxPayable = "2400";

    // Equity
    public const string RetainedEarnings = "3200";
    public const string CurrentYearEarnings = "3500";

    // Revenue
    public const string FoodSales = "4030";
    public const string BeverageSales = "4040";

    // Operating expenses
    public const string SalaryExpense = "5110";
    public const string OvertimeExpense = "5120";
    public const string DepreciationExpense = "5250";

    // Cost of goods sold
    public const string FoodCost = "6010";
    public const string BeverageCost = "6020";

    // Fallback
    public const string Suspense = "9000";
}
