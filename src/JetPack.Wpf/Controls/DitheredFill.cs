using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JetPack.Wpf.Controls;

/// <summary>
/// Fills its area with <see cref="Fill"/>. A <see cref="LinearGradientBrush"/> is drawn as a
/// dithered bitmap on whole device pixels, so a gradient with few colour steps over a large area
/// (the flyout panel) shows no bands. Other brushes are drawn as they are.
/// </summary>
/// <remarks>
/// The bitmap follows the brush's stops, start and end points, mapping mode, spread method and
/// opacity; its <c>Transform</c> and <c>RelativeTransform</c> are ignored. A brush that changes
/// after it is drawn is not picked up until the element is resized or <see cref="Fill"/> is set again.
/// </remarks>
public class DitheredFill : FrameworkElement
{
    private const int Steps = 1024;    // resolution of the colour lookup along the gradient

    private BitmapSource? bitmap;
    private Brush? bitmapFill;
    private DpiScale bitmapDpi;

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(DitheredFill), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush? Fill
    {
        get => (Brush?)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        var bounds = new Rect(RenderSize);
        if (Fill is not LinearGradientBrush brush || brush.GradientStops.Count == 0)
        {
            bitmap = null;
            dc.DrawRectangle(Fill, null, bounds);
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        var width = (int)Math.Round(bounds.Width * dpi.DpiScaleX);
        var height = (int)Math.Round(bounds.Height * dpi.DpiScaleY);
        if (width <= 0 || height <= 0)
        {
            return;
        }
        if (bitmap == null || bitmap.PixelWidth != width || bitmap.PixelHeight != height
            || !ReferenceEquals(bitmapFill, brush) || !bitmapDpi.Equals(dpi))
        {
            bitmap = Render(brush, width, height, dpi);
            bitmapFill = brush;
            bitmapDpi = dpi;
        }
        dc.DrawImage(bitmap, bounds);
    }

    private static BitmapSource Render(LinearGradientBrush brush, int width, int height, DpiScale dpi)
    {
        var lookup = Lookup(brush);

        // Gradient position t = a*x + b*y + c for the pixel centre (x, y), in the brush's own
        // coordinates: 0-1 across the area, or device-independent pixels.
        double sx = 1.0 / width, sy = 1.0 / height;
        if (brush.MappingMode == BrushMappingMode.Absolute)
        {
            sx = 1 / dpi.DpiScaleX;
            sy = 1 / dpi.DpiScaleY;
        }
        Point start = brush.StartPoint, end = brush.EndPoint;
        double dx = end.X - start.X, dy = end.Y - start.Y;
        var length2 = dx * dx + dy * dy;
        if (length2 == 0)
        {
            length2 = 1;
        }
        var a = dx * sx / length2;
        var b = dy * sy / length2;
        var c = ((0.5 * sx - start.X) * dx + (0.5 * sy - start.Y) * dy) / length2;

        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = (int)(Spread(a * x + b * y + c, brush.SpreadMethod) * (Steps - 1) + 0.5) * 4;
                var noise = Dither.Noise(x, y);
                var p = (y * width + x) * 4;
                for (var k = 0; k < 4; k++)
                {
                    pixels[p + k] = (byte)Math.Clamp(Math.Floor(lookup[i + k] + noise), 0, 255);
                }
            }
        }
        var result = BitmapSource.Create(width, height, 96 * dpi.DpiScaleX, 96 * dpi.DpiScaleY,
            PixelFormats.Pbgra32, null, pixels, width * 4);
        result.Freeze();
        return result;
    }

    private static double Spread(double t, GradientSpreadMethod method) => method switch
    {
        GradientSpreadMethod.Repeat => t - Math.Floor(t),
        GradientSpreadMethod.Reflect => 1 - Math.Abs(t - 2 * Math.Floor(t / 2) - 1),
        _ => Math.Clamp(t, 0, 1),
    };

    /// <summary>Premultiplied B, G, R, A in 0-255 (not rounded) at <see cref="Steps"/> points along the gradient.</summary>
    private static double[] Lookup(LinearGradientBrush brush)
    {
        var stops = brush.GradientStops.OrderBy(s => s.Offset).ToArray();
        var lookup = new double[Steps * 4];
        var next = 0;
        for (var i = 0; i < Steps; i++)
        {
            var t = i / (double)(Steps - 1);
            while (next < stops.Length && stops[next].Offset < t)
            {
                next++;
            }
            Color from = stops[Math.Max(next - 1, 0)].Color, to = stops[Math.Min(next, stops.Length - 1)].Color;
            double f0 = stops[Math.Max(next - 1, 0)].Offset, f1 = stops[Math.Min(next, stops.Length - 1)].Offset;
            var f = f1 > f0 ? Math.Clamp((t - f0) / (f1 - f0), 0, 1) : 0;

            var alpha = (from.A + (to.A - from.A) * f) / 255 * brush.Opacity;
            lookup[i * 4] = (from.B + (to.B - from.B) * f) * alpha;
            lookup[i * 4 + 1] = (from.G + (to.G - from.G) * f) * alpha;
            lookup[i * 4 + 2] = (from.R + (to.R - from.R) * f) * alpha;
            lookup[i * 4 + 3] = alpha * 255;
        }
        return lookup;
    }
}
