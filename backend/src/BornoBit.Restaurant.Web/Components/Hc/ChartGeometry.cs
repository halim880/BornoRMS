using System.Globalization;
using System.Text;

namespace BornoBit.Restaurant.Web.Components.Hc;

/// <summary>
/// Helpers shared by the small chart components (sparkline, area chart).
/// </summary>
internal static class ChartGeometry
{
    /// <summary>
    /// Build a smooth SVG path through the supplied points using a cubic
    /// Catmull-Rom-like curve (converted to cubic Béziers). Tension defaults
    /// to a soft 0.18 — high enough to round corners, low enough to avoid
    /// overshoot or wobble.
    /// </summary>
    public static string SmoothPath(IReadOnlyList<(double X, double Y)> pts, double tension = 0.18)
    {
        if (pts.Count < 2) return string.Empty;
        var ci = CultureInfo.InvariantCulture;
        var sb = new StringBuilder(pts.Count * 32);

        sb.Append("M ").Append(pts[0].X.ToString("F2", ci)).Append(' ').Append(pts[0].Y.ToString("F2", ci));

        for (int i = 0; i < pts.Count - 1; i++)
        {
            var p0 = i == 0 ? pts[0] : pts[i - 1];
            var p1 = pts[i];
            var p2 = pts[i + 1];
            var p3 = i + 2 < pts.Count ? pts[i + 2] : pts[i + 1];

            var c1x = p1.X + (p2.X - p0.X) * tension;
            var c1y = p1.Y + (p2.Y - p0.Y) * tension;
            var c2x = p2.X - (p3.X - p1.X) * tension;
            var c2y = p2.Y - (p3.Y - p1.Y) * tension;

            sb.Append(" C ")
                .Append(c1x.ToString("F2", ci)).Append(' ').Append(c1y.ToString("F2", ci)).Append(", ")
                .Append(c2x.ToString("F2", ci)).Append(' ').Append(c2y.ToString("F2", ci)).Append(", ")
                .Append(p2.X.ToString("F2", ci)).Append(' ').Append(p2.Y.ToString("F2", ci));
        }

        return sb.ToString();
    }
}
