using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace JetPack.Wpf.Controls;

/// <summary>
/// The text of a <see cref="JetPackGridColumn"/> cell while <see cref="JetPackGrid.EnableLightweightTextRendering"/>
/// is on: one line drawn as glyph runs, with the search hits marked, cut off where the cell ends.
/// </summary>
/// <remarks>
/// A TextBlock formats its text through WPF's line layout for every new value, which is most of the
/// cost of a recycled row while the grid scrolls. This element draws the font's glyphs itself, with
/// the widths, kerning and line height a TextBlock would use (measured once per font, size and
/// character). Text with characters the font has no glyph for (other scripts, emoji, combining marks)
/// is drawn as <see cref="FormattedText"/>, which falls back to other fonts.
/// </remarks>
public sealed class GridCellText : FrameworkElement
{
    private static readonly Dictionary<Typeface, Face?> Faces = [];

    // GlyphRun's public constructor always makes an Ideal-mode run, which renders the glyphs unhinted
    // (heavier than a TextBlock's) under TextFormattingMode Display. TextBlock creates its runs with
    // this internal method; without it (another WPF version) the public constructor is used.
    private static readonly CreateGlyphRun? CreateRun = FindCreateRun();

    private delegate GlyphRun? CreateGlyphRun(GlyphTypeface glyphTypeface, int bidiLevel, bool isSideways, double renderingEmSize,
        float pixelsPerDip, IList<ushort> glyphIndices, Point baselineOrigin, IList<double> advanceWidths, IList<Point>? glyphOffsets,
        IList<char>? characters, string? deviceFontName, IList<ushort>? clusterMap, IList<bool>? caretStops, XmlLanguage? language,
        TextFormattingMode textFormattingMode);
    private static readonly Dictionary<(Typeface, double Size, TextFormattingMode Mode, double Dpi), Metrics> Sizes = [];

    private string text = string.Empty;
    private IReadOnlyList<(int Start, int End)>? hits;
    private Brush? hitBackground;
    private Brush? hitForeground;
    private TextAlignment textAlignment;
    private Layout? layout;

    static GridCellText()
    {
        SnapsToDevicePixelsProperty.OverrideMetadata(typeof(GridCellText), new FrameworkPropertyMetadata(true));
    }

    public static readonly DependencyProperty ForegroundProperty = TextElement.ForegroundProperty.AddOwner(typeof(GridCellText),
        new FrameworkPropertyMetadata(SystemColors.ControlTextBrush, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public static readonly DependencyProperty FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner(typeof(GridCellText),
        new FrameworkPropertyMetadata(SystemFonts.MessageFontFamily, FrameworkPropertyMetadataOptions.Inherits, OnFontChanged));

    public FontFamily FontFamily
    {
        get => (FontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public static readonly DependencyProperty FontSizeProperty = TextElement.FontSizeProperty.AddOwner(typeof(GridCellText),
        new FrameworkPropertyMetadata(SystemFonts.MessageFontSize, FrameworkPropertyMetadataOptions.Inherits, OnFontChanged));

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public static readonly DependencyProperty FontStyleProperty = TextElement.FontStyleProperty.AddOwner(typeof(GridCellText),
        new FrameworkPropertyMetadata(SystemFonts.MessageFontStyle, FrameworkPropertyMetadataOptions.Inherits, OnFontChanged));

    public FontStyle FontStyle
    {
        get => (FontStyle)GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    public static readonly DependencyProperty FontWeightProperty = TextElement.FontWeightProperty.AddOwner(typeof(GridCellText),
        new FrameworkPropertyMetadata(SystemFonts.MessageFontWeight, FrameworkPropertyMetadataOptions.Inherits, OnFontChanged));

    public FontWeight FontWeight
    {
        get => (FontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public static readonly DependencyProperty FontStretchProperty = TextElement.FontStretchProperty.AddOwner(typeof(GridCellText),
        new FrameworkPropertyMetadata(FontStretches.Normal, FrameworkPropertyMetadataOptions.Inherits, OnFontChanged));

    public FontStretch FontStretch
    {
        get => (FontStretch)GetValue(FontStretchProperty);
        set => SetValue(FontStretchProperty, value);
    }

    /// <summary>The text shown (set by the column).</summary>
    public string Text => text;

    /// <summary>The marked parts of <see cref="Text"/>, as start and end indexes; null when nothing is marked.</summary>
    public IReadOnlyList<(int Start, int End)>? Hits => hits;

    public TextAlignment TextAlignment
    {
        get => textAlignment;
        set
        {
            if (textAlignment != value)
            {
                textAlignment = value;
                InvalidateVisual();
            }
        }
    }

    /// <summary>
    /// Shows a new text; the column calls it when the value or the search changes. Without
    /// <paramref name="measure"/> only the drawing changes (the thumb-drag preview: the element is
    /// stretched over its cell, so its size does not depend on the text).
    /// </summary>
    internal void SetText(string value, IReadOnlyList<(int Start, int End)>? marked, Brush? background, Brush? foreground, bool measure = true)
    {
        if (value == text && marked == null && hits == null)
        {
            return;
        }
        if (value != text)
        {
            text = value;
            layout = null;
            if (measure)
            {
                InvalidateMeasure();
            }
        }
        hits = marked;
        hitBackground = background;
        hitForeground = foreground;
        InvalidateVisual();
    }

    private static void OnFontChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var cell = (GridCellText)d;
        cell.layout = null;
        cell.InvalidateMeasure();
        cell.InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        layout ??= CreateLayout();
        return new Size(Math.Min(layout.Width, availableSize.Width), layout.LineHeight);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var width = RenderSize.Width;
        if (text.Length == 0 || width <= 0)
        {
            return;
        }
        layout ??= CreateLayout();
        var x = textAlignment switch
        {
            TextAlignment.Right => width - layout.Width,
            TextAlignment.Center => (width - layout.Width) / 2,
            _ => 0,
        };
        // Text wider than the cell is cut off at its edge, as a TextBlock is.
        var clipped = x < 0 || layout.Width > width + 0.01;
        if (clipped)
        {
            dc.PushClip(new RectangleGeometry(new Rect(0, 0, width, Math.Max(RenderSize.Height, layout.LineHeight))));
        }
        if (layout.Formatted is { } formatted)
        {
            RenderFormatted(dc, formatted, x);
        }
        else
        {
            RenderGlyphs(dc, layout, x);
        }
        if (clipped)
        {
            dc.Pop();
        }
    }

    private void RenderGlyphs(DrawingContext dc, Layout layout, double x)
    {
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var baseline = Math.Round(layout.Baseline * dpi) / dpi;

        // The text in parts, the marked ones on their background and in their own brush.
        var count = layout.Glyphs.Length;
        var position = 0;
        if (hits != null)
        {
            foreach (var (start, end) in hits)
            {
                var from = Math.Min(start, count);
                var to = Math.Min(end, count);
                x = Draw(dc, layout, position, from, x, baseline, Foreground, dpi);
                if (to > from && hitBackground != null)
                {
                    dc.DrawRectangle(hitBackground, null, new Rect(x, layout.HitTop, Sum(layout, from, to), layout.HitHeight));
                }
                x = Draw(dc, layout, from, to, x, baseline, hitForeground ?? Foreground, dpi);
                position = Math.Max(position, to);
            }
        }
        Draw(dc, layout, position, count, x, baseline, Foreground, dpi);
    }

    private static double Sum(Layout layout, int from, int to)
    {
        var sum = 0.0;
        for (var i = from; i < to; i++)
        {
            sum += layout.Advances[i];
        }
        return sum;
    }

    // Draws the glyphs from..to at x and returns where they end.
    private static double Draw(DrawingContext dc, Layout layout, int from, int to, double x, double baseline, Brush brush, double dpi)
    {
        if (to <= from)
        {
            return x;
        }
        var glyphs = new ushort[to - from];
        var advances = new double[to - from];
        Array.Copy(layout.Glyphs, from, glyphs, 0, glyphs.Length);
        Array.Copy(layout.Advances, from, advances, 0, advances.Length);
        var origin = new Point(x, baseline);
        var run = layout.Mode == TextFormattingMode.Display
            ? CreateRun?.Invoke(layout.Face, 0, false, layout.EmSize, (float)dpi, glyphs, origin, advances, null, null, null, null, null, null, layout.Mode)
            : null;
        dc.DrawGlyphRun(brush, run ?? new GlyphRun(layout.Face, 0, false, layout.EmSize, (float)dpi, glyphs, origin, advances,
            null, null, null, null, null, null));
        return x + Sum(layout, from, to);
    }

    private static CreateGlyphRun? FindCreateRun()
    {
        try
        {
            return typeof(GlyphRun).GetMethod("TryCreate", BindingFlags.NonPublic | BindingFlags.Static)?.CreateDelegate<CreateGlyphRun>();
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private void RenderFormatted(DrawingContext dc, FormattedText formatted, double x)
    {
        formatted.SetForegroundBrush(Foreground);
        if (hits != null)
        {
            foreach (var (start, end) in hits)
            {
                if (end > start && start < text.Length)
                {
                    var length = Math.Min(end, text.Length) - start;
                    if (hitBackground != null && formatted.BuildHighlightGeometry(new Point(x, 0), start, length) is { } area)
                    {
                        dc.DrawGeometry(hitBackground, null, area);
                    }
                    if (hitForeground != null)
                    {
                        formatted.SetForegroundBrush(hitForeground, start, length);
                    }
                }
            }
        }
        dc.DrawText(formatted, new Point(x, 0));
    }

    private Layout CreateLayout()
    {
        var size = FontSize;
        var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
        var mode = TextOptions.GetTextFormattingMode(this);
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var metrics = MetricsOf(typeface, size, mode, dpi);
        if (FaceOf(typeface) is { } face && TryGetGlyphs(face, metrics, text, out var glyphs, out var advances, out var width))
        {
            return new Layout(face.Glyphs, size, mode, metrics.LineHeight, metrics.Baseline, metrics.HitTop, metrics.HitHeight, glyphs, advances, width, null);
        }

        var formatted = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeface, size, Foreground, null, mode, dpi);
        return new Layout(null!, size, mode, Math.Max(metrics.LineHeight, formatted.Height), metrics.Baseline, 0, 0, [], [],
            formatted.WidthIncludingTrailingWhitespace, formatted);
    }

    private static Face? FaceOf(Typeface typeface)
    {
        if (!Faces.TryGetValue(typeface, out var face))
        {
            Faces[typeface] = face = typeface.TryGetGlyphTypeface(out var found) ? new Face(found) : null;
        }
        return face;
    }

    private static Metrics MetricsOf(Typeface typeface, double size, TextFormattingMode mode, double dpi)
    {
        var key = (typeface, size, mode, dpi);
        if (!Sizes.TryGetValue(key, out var metrics))
        {
            Sizes[key] = metrics = new Metrics(typeface, size, mode, dpi);
        }
        return metrics;
    }

    // The glyphs one to one with the characters, or false when that is not enough to draw the text.
    private static bool TryGetGlyphs(Face face, Metrics metrics, string value, out ushort[] glyphs, out double[] advances, out double width)
    {
        glyphs = new ushort[value.Length];
        advances = new double[value.Length];
        width = 0;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c < ' ' || char.IsSurrogate(c) || IsCombining(c) || !face.TryGet(c, out var glyph))
            {
                return false;
            }
            glyphs[i] = glyph;
            advances[i] = metrics.Advance(c);
            if (i > 0)
            {
                advances[i - 1] += metrics.Kerning(value[i - 1], c);
                width += advances[i - 1];
            }
        }
        if (value.Length > 0)
        {
            width += advances[^1];
        }
        return true;
    }

    private static bool IsCombining(char c) => c >= '̀' && char.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark
        or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark or UnicodeCategory.Format;

    protected override AutomationPeer OnCreateAutomationPeer() => new GridCellTextAutomationPeer(this);

    // A font's glyph per character, read once: GlyphTypeface reads its tables on every lookup.
    private sealed class Face(GlyphTypeface glyphs)
    {
        private readonly Dictionary<char, ushort?> known = [];

        public GlyphTypeface Glyphs => glyphs;

        public bool TryGet(char c, out ushort glyph)
        {
            if (!known.TryGetValue(c, out var entry))
            {
                known[c] = entry = glyphs.CharacterToGlyphMap.TryGetValue(c, out var found) ? found : null;
            }
            glyph = entry ?? 0;
            return entry != null;
        }
    }

    // The line, the character widths and the kerning of a font at one size, as a TextBlock lays them
    // out (with Display formatting WPF uses the font's hinted widths and a taller line). Each is
    // measured once with FormattedText, so a cell only adds up widths that are known already.
    private sealed class Metrics
    {
        private readonly Typeface typeface;
        private readonly double size;
        private readonly TextFormattingMode mode;
        private readonly double dpi;
        private readonly Dictionary<char, double> advances = [];
        private readonly Dictionary<(char, char), double> kerning = [];

        public Metrics(Typeface typeface, double size, TextFormattingMode mode, double dpi)
        {
            this.typeface = typeface;
            this.size = size;
            this.mode = mode;
            this.dpi = dpi;
            var line = Format("0");
            LineHeight = line.Height;
            Baseline = line.Baseline;
            // A marked run's background covers the font's ascent and descent around the baseline, not
            // the whole line (taller with Display formatting).
            if (typeface.TryGetGlyphTypeface(out var face))
            {
                HitTop = Baseline - face.Baseline * size;
                HitHeight = face.Height * size;
            }
            else
            {
                HitHeight = LineHeight;
            }
        }

        public double LineHeight { get; }

        public double Baseline { get; }

        public double HitTop { get; }

        public double HitHeight { get; }

        public double Advance(char c)
        {
            if (!advances.TryGetValue(c, out var advance))
            {
                advances[c] = advance = Format(c.ToString()).WidthIncludingTrailingWhitespace;
            }
            return advance;
        }

        // How far the font's kerning moves the second character of a pair (negative: closer).
        public double Kerning(char first, char second)
        {
            if (!kerning.TryGetValue((first, second), out var adjust))
            {
                adjust = Format(new string([first, second])).WidthIncludingTrailingWhitespace - Advance(first) - Advance(second);
                kerning[(first, second)] = adjust = Math.Abs(adjust) < 0.001 ? 0 : adjust;
            }
            return adjust;
        }

        private FormattedText Format(string value) =>
            new(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, size, Brushes.Black, null, mode, dpi);
    }

    private sealed record Layout(GlyphTypeface Face, double EmSize, TextFormattingMode Mode, double LineHeight, double Baseline, double HitTop, double HitHeight,
        ushort[] Glyphs, double[] Advances, double Width, FormattedText? Formatted);

    private sealed class GridCellTextAutomationPeer(GridCellText owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

        protected override string GetClassNameCore() => nameof(GridCellText);

        protected override string GetNameCore() => ((GridCellText)Owner).Text;
    }
}
