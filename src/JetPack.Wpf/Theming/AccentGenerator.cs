using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace JetPack.Wpf.Theming;

/// <summary>
/// Builds a complete accent dictionary from one colour. The lightness and
/// saturation offsets are measured from the original green accent, so a
/// generated accent keeps the same depth and highlights.
/// </summary>
public static class AccentGenerator
{
    public static ResourceDictionary Create(Color solid, string? name = null)
    {
        var (h, s, l) = ToHsl(solid);
        Color Shade(double dl, double sMul = 1, double? fixedS = null) =>
            FromHsl(h, Clamp(fixedS ?? s * sMul), Clamp(l + dl));

        var highlight = Shade(0.10, fixedS: Math.Max(s, 0.85));
        var shadow = FromHsl(h, Clamp(s), 0.12);

        var d = new ResourceDictionary
        {
            ["JetPack.Accent.Name"] = name ?? solid.ToString(),
            ["JetPack.Colors.Accent"] = solid,
            ["JetPack.Colors.Accent.Highlight"] = highlight,
            ["JetPack.Colors.Accent.TextShadow"] = shadow,
            ["JetPack.Brushes.Accent.Selection"] = Vertical((Shade(0.06, 0.92), 0), (solid, 1)),
            ["JetPack.Brushes.Accent"] = Vertical((Shade(0.21, 0.74), 0), (Shade(0.02, 1.05), 1)),
            ["JetPack.Brushes.Accent.MouseOver"] = Vertical((Shade(0.05), 0), (Shade(0.22, 0.8), 1)),
            ["JetPack.Brushes.Accent.Pressed"] = Vertical((Shade(-0.06), 0.31), (Shade(-0.09), 0.78)),
            ["JetPack.Brushes.Accent.Highlight"] = Freeze(new SolidColorBrush(highlight)),
            ["JetPack.Brushes.Accent.Solid"] = Freeze(new SolidColorBrush(solid)),
            ["JetPack.Brushes.Accent.Link"] = Freeze(new SolidColorBrush(Shade(0.16, fixedS: 1))),
            ["JetPack.Brushes.Accent.Foreground"] = Brushes.White,
            ["JetPack.Brushes.Accent.Button.Normal"] = Vertical((Shade(0.05, 0.9), 0.176), (Shade(-0.04), 0.894)),
            ["JetPack.Brushes.Accent.Button.MouseOver"] = Vertical((Shade(0.07, 0.95), 0.228), (Shade(0.01), 0.849)),
            ["JetPack.Brushes.Accent.Button.Pressed"] = Vertical((Shade(-0.06), 0.31), (Shade(-0.09), 0.78)),
            ["JetPack.Brushes.Accent.Button.Highlight"] = Freeze(new SolidColorBrush(Shade(0.14, 0.7))),
            ["JetPack.Effects.Accent.TextShadow"] = Freeze(new DropShadowEffect { Color = shadow, Direction = 90, ShadowDepth = 1, BlurRadius = 0, Opacity = 0.5 }),
            ["JetPack.Brushes.Focus"] = Freeze(new SolidColorBrush(highlight)),
            ["JetPack.Brushes.Focus.Border"] = Freeze(new SolidColorBrush(solid)),
        };
        return d;
    }

    private static T Freeze<T>(T f) where T : Freezable
    {
        f.Freeze();
        return f;
    }

    private static LinearGradientBrush Vertical(params (Color Color, double Offset)[] stops)
    {
        var b = new LinearGradientBrush { StartPoint = new Point(0.5, 0), EndPoint = new Point(0.5, 1) };
        foreach (var (c, o) in stops)
        {
            b.GradientStops.Add(new GradientStop(c, o));
        }
        return Freeze(b);
    }

    private static double Clamp(double v) => Math.Max(0, Math.Min(1, v));

    internal static (double H, double S, double L) ToHsl(Color c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
        double l = (max + min) / 2, h = 0, s = 0;
        if (max != min)
        {
            var d = max - min;
            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            if (max == r) h = (g - b) / d + (g < b ? 6 : 0);
            else if (max == g) h = (b - r) / d + 2;
            else h = (r - g) / d + 4;
            h /= 6;
        }
        return (h, s, l);
    }

    internal static Color FromHsl(double h, double s, double l)
    {
        double r, g, b;
        if (s == 0)
        {
            r = g = b = l;
        }
        else
        {
            double q = l < 0.5 ? l * (1 + s) : l + s - l * s, p = 2 * l - q;
            r = Hue(p, q, h + 1.0 / 3);
            g = Hue(p, q, h);
            b = Hue(p, q, h - 1.0 / 3);
        }
        return Color.FromRgb((byte)Math.Round(r * 255), (byte)Math.Round(g * 255), (byte)Math.Round(b * 255));
    }

    private static double Hue(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6) return p + (q - p) * 6 * t;
        if (t < 0.5) return q;
        if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
        return p;
    }
}
