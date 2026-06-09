using BornoBit.Restaurant.Domain.Numbering;

namespace BornoBit.Restaurant.Web.Components.Pages.Admin.NumberingScopes;

public class NumberingScopeFormModel
{
    public Guid? Id { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Prefix { get; set; }
    public NumberingCadence Cadence { get; set; } = NumberingCadence.Daily;
    public byte Digits { get; set; } = 4;
    public bool ResetByOutlet { get; set; }
}
