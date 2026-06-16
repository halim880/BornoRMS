using System.Text;

namespace BornoBit.PrintAgent.Printing;

/// <summary>
/// Minimal ESC/POS command builder for thermal receipt printers. Text is encoded as
/// Latin-1 (single-byte, always available) — enough for English/numbers/currency codes.
/// </summary>
public sealed class EscPosBuilder(int widthChars)
{
    private static readonly Encoding Enc = Encoding.Latin1;
    private readonly MemoryStream _buf = new();
    public int Width { get; } = Math.Clamp(widthChars, 24, 64);

    public EscPosBuilder Init()        => Raw(0x1B, 0x40);                 // ESC @  — reset
    public EscPosBuilder AlignLeft()   => Raw(0x1B, 0x61, 0x00);
    public EscPosBuilder AlignCenter() => Raw(0x1B, 0x61, 0x01);
    public EscPosBuilder AlignRight()  => Raw(0x1B, 0x61, 0x02);
    public EscPosBuilder Bold(bool on) => Raw(0x1B, 0x45, (byte)(on ? 1 : 0));

    /// <summary>GS ! n — width/height magnification (1 = normal, 2 = double).</summary>
    public EscPosBuilder Size(int widthMul, int heightMul)
    {
        var w = (byte)(Math.Clamp(widthMul, 1, 8) - 1);
        var h = (byte)(Math.Clamp(heightMul, 1, 8) - 1);
        return Raw(0x1D, 0x21, (byte)((w << 4) | h));
    }

    public EscPosBuilder Text(string? s)
    {
        if (!string.IsNullOrEmpty(s))
        {
            var bytes = Enc.GetBytes(s);
            _buf.Write(bytes, 0, bytes.Length);
        }
        return this;
    }

    public EscPosBuilder Line(string? s = null) => Text(s).Raw(0x0A);

    /// <summary>A full-width separator row of the given char (default '-').</summary>
    public EscPosBuilder Rule(char c = '-') => Line(new string(c, Width));

    /// <summary>Left text and right text padded to the full width on one line (wraps if too long).</summary>
    public EscPosBuilder LeftRight(string left, string right)
    {
        left ??= "";
        right ??= "";
        var space = Width - left.Length - right.Length;
        if (space >= 1)
            return Line(left + new string(' ', space) + right);
        // Too wide: drop right onto its own right-aligned line.
        Line(left);
        var pad = Math.Max(0, Width - right.Length);
        return Line(new string(' ', pad) + right);
    }

    public EscPosBuilder Feed(int lines = 1) => Raw(0x1B, 0x64, (byte)Math.Clamp(lines, 0, 255));

    /// <summary>GS V — cut the paper. Partial cut by default; full cut when requested.</summary>
    public EscPosBuilder Cut(bool full) => Feed(3).Raw(0x1D, 0x56, (byte)(full ? 0x00 : 0x01));

    /// <summary>ESC p 0 — kick the cash drawer on pin 2 (the common wiring).</summary>
    public EscPosBuilder OpenDrawer() => Raw(0x1B, 0x70, 0x00, 0x19, 0xFA);

    public EscPosBuilder Raw(params byte[] bytes)
    {
        _buf.Write(bytes, 0, bytes.Length);
        return this;
    }

    public byte[] ToArray() => _buf.ToArray();
}
