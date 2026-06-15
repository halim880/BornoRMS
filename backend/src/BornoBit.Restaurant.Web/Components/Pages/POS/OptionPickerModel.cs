using BornoBit.Restaurant.Application.Products;

namespace BornoBit.Restaurant.Web.Components.Pages.POS;

/// <summary>Dialog model for choosing a product's modifiers / add-ons when adding it to a cart.</summary>
public class OptionPickerModel
{
    public string ProductName { get; set; } = string.Empty;
    public string Currency { get; set; } = "Tk";
    public List<OptionGroupChoice> Groups { get; set; } = new();

    /// <summary>The options the user ticked, flattened across groups.</summary>
    public IEnumerable<(Guid OptionId, string Name, decimal PriceDelta)> Selected() =>
        Groups.SelectMany(g => g.Options.Where(o => o.Selected).Select(o => (o.Id, o.Name, o.PriceDelta)));

    /// <summary>Validates each group's required / min / max rules. Returns the first violation, or null when valid.</summary>
    public string? Validate()
    {
        foreach (var g in Groups)
        {
            var n = g.Options.Count(o => o.Selected);
            if (n < g.Min) return g.Min == 1 ? $"Choose an option for '{g.Name}'." : $"Choose at least {g.Min} for '{g.Name}'.";
            if (n > g.Max) return $"Choose at most {g.Max} for '{g.Name}'.";
        }
        return null;
    }
}

public class OptionGroupChoice
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSingle { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }
    public List<OptionChoice> Options { get; set; } = new();

    public bool IsRequired => Min >= 1;

    public static OptionGroupChoice From(ProductOptionGroupDto g) => new()
    {
        Id = g.Id,
        Name = g.Name,
        IsSingle = g.IsSingleSelect,
        Min = g.MinSelections,
        Max = g.MaxSelections,
        Options = g.Options.Select(o => new OptionChoice { Id = o.Id, Name = o.Name, PriceDelta = o.PriceDelta }).ToList()
    };

    /// <summary>Single-select: ticking one option clears the rest in this group.</summary>
    public void Pick(OptionChoice option, bool selected)
    {
        if (IsSingle && selected)
            foreach (var o in Options) o.Selected = false;
        option.Selected = selected;
    }
}

public class OptionChoice
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceDelta { get; set; }
    public bool Selected { get; set; }
}
