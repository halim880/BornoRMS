namespace BornoBit.Restaurant.Application.Products;

// ── Modifier / add-on option groups ──────────────────────────────────────────────────

public record ProductOptionDto(
    Guid Id,
    string Name,
    string? BanglaName,
    decimal PriceDelta,
    int DisplayOrder);

public record ProductOptionGroupDto(
    Guid Id,
    string Name,
    string? BanglaName,
    int MinSelections,
    int MaxSelections,
    int DisplayOrder,
    IReadOnlyList<ProductOptionDto> Options)
{
    public bool IsRequired => MinSelections >= 1;
    public bool IsSingleSelect => MaxSelections <= 1;
}

/// <summary>Option row sent from the form; Id is null for new rows.</summary>
public record OptionInput(Guid? Id, string Name, string? BanglaName, decimal PriceDelta, int DisplayOrder);

/// <summary>Option-group row sent from the form; Id is null for new rows.</summary>
public record OptionGroupInput(
    Guid? Id,
    string Name,
    string? BanglaName,
    int MinSelections,
    int MaxSelections,
    int DisplayOrder,
    IReadOnlyList<OptionInput> Options);

// ── Combos ───────────────────────────────────────────────────────────────────────────

public record ComboComponentDto(
    Guid Id,
    Guid ComponentProductId,
    string ComponentName,
    int Quantity,
    int DisplayOrder);

/// <summary>Combo component row sent from the form; Id is null for new rows.</summary>
public record ComboComponentInput(Guid? Id, Guid ComponentProductId, int Quantity, int DisplayOrder);
