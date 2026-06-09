using BornoBit.Restaurant.Domain.Common;

namespace BornoBit.Restaurant.Domain.Numbering;

public class NumberingScope : AuditableEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Prefix { get; private set; } = default!;
    public NumberingCadence Cadence { get; private set; }
    public byte Digits { get; private set; }
    public bool ResetByOutlet { get; private set; }
    public bool IsActive { get; private set; } = true;

    private NumberingScope() { }

    public static NumberingScope Create(
        string code,
        string name,
        string prefix,
        NumberingCadence cadence,
        byte digits,
        bool resetByOutlet)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("Prefix is required.", nameof(prefix));
        if (digits < 1 || digits > 10) throw new ArgumentOutOfRangeException(nameof(digits), "Digits must be 1..10.");

        return new NumberingScope
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Prefix = prefix.Trim(),
            Cadence = cadence,
            Digits = digits,
            ResetByOutlet = resetByOutlet,
            IsActive = true
        };
    }

    public void UpdateDetails(string name, string prefix, NumberingCadence cadence, byte digits, bool resetByOutlet)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("Prefix is required.", nameof(prefix));
        if (digits < 1 || digits > 10) throw new ArgumentOutOfRangeException(nameof(digits));

        Name = name.Trim();
        Prefix = prefix.Trim();
        Cadence = cadence;
        Digits = digits;
        ResetByOutlet = resetByOutlet;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public string BuildPeriodKey(DateTime utcNow) => Cadence switch
    {
        NumberingCadence.Yearly => utcNow.ToString("yy"),
        NumberingCadence.Monthly => utcNow.ToString("yyMM"),
        NumberingCadence.Daily => utcNow.ToString("yyMMdd"),
        _ => throw new InvalidOperationException($"Unknown cadence {Cadence}.")
    };

    public string FormatNumber(string periodKey, long sequence)
        => $"{Prefix}{periodKey}{sequence.ToString("D" + Digits)}";
}
