using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JetPack.Wpf.Controls;

/// <summary>
/// The darkened edges of the JetPack window, drawn in code: a shade of fixed width along every
/// edge, dithered so the gradient does not band. <see cref="JetPackWindow"/> puts one between
/// the ground and the content.
/// </summary>
public class Vignette : FrameworkElement
{
    // Shape of the shade (the numbers of the former Vignette.png)
    private const double Curve = 1.9;       // falloff exponent; higher fades faster near the edge
    private const double CornerNorm = 1.6;  // p-norm used to join two edges; lower darkens corners more
    private const int TileLength = 256;     // device pixels of edge in one bitmap, repeated along the edge

    private Tiles? tiles;

    static Vignette()
    {
        IsHitTestVisibleProperty.OverrideMetadata(typeof(Vignette), new UIPropertyMetadata(false));
    }

    public static readonly DependencyProperty ReachProperty = DependencyProperty.Register(
        nameof(Reach), typeof(double), typeof(Vignette), new FrameworkPropertyMetadata(180d, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>How far in from each edge the shade reaches, in device-independent pixels.</summary>
    public double Reach
    {
        get => (double)GetValue(ReachProperty);
        set => SetValue(ReachProperty, value);
    }

    public static readonly DependencyProperty StrengthProperty = DependencyProperty.Register(
        nameof(Strength), typeof(double), typeof(Vignette), new FrameworkPropertyMetadata(0.3, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Opacity of the shade at the middle of an edge (the corners get darker).</summary>
    public double Strength
    {
        get => (double)GetValue(StrengthProperty);
        set => SetValue(StrengthProperty, value);
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        var reachPx = (int)Math.Round(Reach * dpi.DpiScaleX);
        if (reachPx <= 0 || Strength <= 0)
        {
            return;
        }
        if (tiles is not { } t || t.ReachPx != reachPx || t.Strength != Strength || !t.Dpi.Equals(dpi))
        {
            tiles = t = new Tiles(reachPx, Strength, dpi);
        }

        // Bitmaps are drawn 1:1 on whole device pixels, so tiles meet without seams.
        double w = ActualWidth, h = ActualHeight;
        var r = reachPx / dpi.DpiScaleX;
        var len = TileLength / dpi.DpiScaleX;
        var rx = Math.Min(r, w / 2);
        var ry = Math.Min(r, h / 2);

        // Corners; in a window narrower than two reaches the shades meet in the middle.
        DrawClipped(dc, t.TopLeft, new Rect(0, 0, r, r), new Rect(0, 0, rx, ry));
        DrawClipped(dc, t.TopRight, new Rect(w - r, 0, r, r), new Rect(w - rx, 0, rx, ry));
        DrawClipped(dc, t.BottomLeft, new Rect(0, h - r, r, r), new Rect(0, h - ry, rx, ry));
        DrawClipped(dc, t.BottomRight, new Rect(w - r, h - r, r, r), new Rect(w - rx, h - ry, rx, ry));

        // Edges, one tile after the other
        if (w > 2 * r)
        {
            dc.PushClip(new RectangleGeometry(new Rect(r, 0, w - 2 * r, ry)));
            for (var x = r; x < w - r; x += len)
            {
                dc.DrawImage(t.Top, new Rect(x, 0, len, r));
            }
            dc.Pop();
            dc.PushClip(new RectangleGeometry(new Rect(r, h - ry, w - 2 * r, ry)));
            for (var x = r; x < w - r; x += len)
            {
                dc.DrawImage(t.Bottom, new Rect(x, h - r, len, r));
            }
            dc.Pop();
        }
        if (h > 2 * r)
        {
            dc.PushClip(new RectangleGeometry(new Rect(0, r, rx, h - 2 * r)));
            for (var y = r; y < h - r; y += len)
            {
                dc.DrawImage(t.Left, new Rect(0, y, r, len));
            }
            dc.Pop();
            dc.PushClip(new RectangleGeometry(new Rect(w - rx, r, rx, h - 2 * r)));
            for (var y = r; y < h - r; y += len)
            {
                dc.DrawImage(t.Right, new Rect(w - r, y, r, len));
            }
            dc.Pop();
        }
    }

    private static void DrawClipped(DrawingContext dc, ImageSource image, Rect target, Rect clip)
    {
        dc.PushClip(new RectangleGeometry(clip));
        dc.DrawImage(image, target);
        dc.Pop();
    }

    /// <summary>
    /// The eight pieces of a shaded frame two reaches plus one tile wide, cut so that the corners
    /// and the edge tiles fit together.
    /// </summary>
    private sealed class Tiles
    {
        public readonly int ReachPx;
        public readonly double Strength;
        public readonly DpiScale Dpi;
        public readonly BitmapSource TopLeft, TopRight, BottomLeft, BottomRight, Top, Bottom, Left, Right;

        private readonly int frame;

        public Tiles(int reachPx, double strength, DpiScale dpi)
        {
            ReachPx = reachPx;
            Strength = strength;
            Dpi = dpi;
            frame = 2 * reachPx + TileLength;

            int r = reachPx, far = reachPx + TileLength;
            TopLeft = Cut(0, 0, r, r);
            TopRight = Cut(far, 0, r, r);
            BottomLeft = Cut(0, far, r, r);
            BottomRight = Cut(far, far, r, r);
            Top = Cut(r, 0, TileLength, r);
            Bottom = Cut(r, far, TileLength, r);
            Left = Cut(0, r, r, TileLength);
            Right = Cut(far, r, r, TileLength);
        }

        private BitmapSource Cut(int x0, int y0, int width, int height)
        {
            var pixels = new byte[width * height * 4];
            for (var y = 0; y < height; y++)
            {
                var sy = Shade(y0 + y);
                for (var x = 0; x < width; x++)
                {
                    var sx = Shade(x0 + x);
                    var alpha = sx == 0 ? sy : sy == 0 ? sx
                        : Math.Pow(Math.Pow(sx, CornerNorm) + Math.Pow(sy, CornerNorm), 1 / CornerNorm);

                    // Black, premultiplied, so only alpha is set. Adding noise below one step
                    // before rounding down dithers the gradient instead of leaving bands.
                    var a = Math.Floor(Math.Min(alpha, 1) * 255 + Noise(x0 + x, y0 + y));
                    pixels[(y * width + x) * 4 + 3] = (byte)Math.Clamp(a, 0, 255);
                }
            }
            var bitmap = BitmapSource.Create(width, height, 96 * Dpi.DpiScaleX, 96 * Dpi.DpiScaleY,
                PixelFormats.Pbgra32, null, pixels, width * 4);
            bitmap.Freeze();
            return bitmap;
        }

        /// <summary>Shade from one edge at a frame coordinate; zero beyond the reach.</summary>
        private double Shade(int i)
        {
            var distance = Math.Min(i + 0.5, frame - i - 0.5);
            var t = 1 - distance / ReachPx;
            return t <= 0 ? 0 : Strength * Math.Pow(t, Curve);
        }

        /// <summary>Repeatable noise in [0, 1) from pixel coordinates.</summary>
        private static double Noise(int x, int y)
        {
            var h = (uint)(x * 73856093) ^ (uint)(y * 19349663);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            return (h & 0xFFFF) / 65536.0;
        }
    }
}
