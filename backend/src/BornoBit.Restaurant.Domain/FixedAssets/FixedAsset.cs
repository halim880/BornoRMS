using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.FixedAssets;

public enum DepreciationMethod { StraightLine = 1 }

public enum FixedAssetStatus { Active = 1, FullyDepreciated = 2, Disposed = 3 }

/// <summary>
/// A depreciable capital asset (equipment, furniture, vehicles). Straight-line depreciation is posted
/// monthly by the depreciation run (Dr Depreciation Expense / Cr Accumulated Depreciation). Net book value
/// is cost less accumulated depreciation; depreciation stops once the depreciable base (cost − salvage) is
/// exhausted.
/// </summary>
public class FixedAsset : AuditableEntity
{
    public string AssetNumber { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public Guid AssetGlAccountId { get; private set; }
    public DateTime AcquisitionDate { get; private set; }
    public decimal Cost { get; private set; }
    public decimal SalvageValue { get; private set; }
    public int UsefulLifeMonths { get; private set; }
    public DepreciationMethod Method { get; private set; } = DepreciationMethod.StraightLine;
    public decimal AccumulatedDepreciation { get; private set; }
    public FixedAssetStatus Status { get; private set; } = FixedAssetStatus.Active;
    public DateTime? DisposedOn { get; private set; }

    private readonly List<DepreciationEntry> _entries = new();
    public IReadOnlyCollection<DepreciationEntry> Entries => _entries.AsReadOnly();

    public decimal DepreciableBase => Cost - SalvageValue;
    public decimal NetBookValue => Cost - AccumulatedDepreciation;
    public decimal RemainingDepreciable => Math.Max(0m, DepreciableBase - AccumulatedDepreciation);

    private FixedAsset() { }

    public static FixedAsset Create(
        string assetNumber, string name, Guid assetGlAccountId, DateTime acquisitionDate,
        decimal cost, decimal salvageValue, int usefulLifeMonths, DepreciationMethod method = DepreciationMethod.StraightLine)
    {
        if (string.IsNullOrWhiteSpace(assetNumber)) throw new ArgumentException("Asset number is required.", nameof(assetNumber));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (assetGlAccountId == Guid.Empty) throw new ArgumentException("GL asset account is required.", nameof(assetGlAccountId));
        if (cost <= 0m) throw new ArgumentOutOfRangeException(nameof(cost), "Cost must be greater than zero.");
        if (salvageValue < 0m || salvageValue >= cost) throw new ArgumentOutOfRangeException(nameof(salvageValue), "Salvage must be between 0 and cost.");
        if (usefulLifeMonths <= 0) throw new ArgumentOutOfRangeException(nameof(usefulLifeMonths), "Useful life must be at least one month.");

        return new FixedAsset
        {
            AssetNumber = assetNumber.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            AssetGlAccountId = assetGlAccountId,
            AcquisitionDate = acquisitionDate.Date,
            Cost = cost,
            SalvageValue = salvageValue,
            UsefulLifeMonths = usefulLifeMonths,
            Method = method,
            Status = FixedAssetStatus.Active
        };
    }

    /// <summary>Straight-line monthly charge, capped at what remains depreciable.</summary>
    public decimal MonthlyDepreciation()
    {
        if (Status != FixedAssetStatus.Active) return 0m;
        var perMonth = Math.Round(DepreciableBase / UsefulLifeMonths, 2);
        return Math.Min(perMonth, RemainingDepreciable);
    }

    public DepreciationEntry RecordDepreciation(int year, int month, decimal amount, string? journalReference)
    {
        if (Status != FixedAssetStatus.Active) throw new InvalidOperationException("Only active assets depreciate.");
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount));
        if (amount > RemainingDepreciable + 0.005m) throw new InvalidOperationException("Depreciation exceeds the remaining depreciable base.");

        AccumulatedDepreciation += amount;
        var entry = new DepreciationEntry
        {
            FixedAssetId = Id,
            Year = year,
            Month = month,
            Amount = amount,
            JournalReference = journalReference
        };
        _entries.Add(entry);

        if (RemainingDepreciable <= 0m) Status = FixedAssetStatus.FullyDepreciated;
        return entry;
    }

    public void Dispose(DateTime disposedOn)
    {
        if (Status == FixedAssetStatus.Disposed) throw new InvalidOperationException("Asset is already disposed.");
        Status = FixedAssetStatus.Disposed;
        DisposedOn = disposedOn.Date;
    }
}

/// <summary>One month's depreciation posted against a <see cref="FixedAsset"/> (idempotency + schedule history).</summary>
public class DepreciationEntry : BaseEntity
{
    public Guid FixedAssetId { get; internal set; }
    public int Year { get; internal set; }
    public int Month { get; internal set; }
    public decimal Amount { get; internal set; }
    public string? JournalReference { get; internal set; }
}
