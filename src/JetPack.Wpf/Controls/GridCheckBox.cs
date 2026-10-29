using System.Windows;
using System.Windows.Media;

namespace JetPack.Wpf.Controls;

/// <summary>
/// The check box of a <see cref="JetPackGridCheckBoxColumn"/> cell that is not being edited: the 13px box of
/// <c>JetPack.Styles.CheckBox</c> with its mark, drawn by one element instead of a CheckBox and its template.
/// </summary>
/// <remarks>
/// The layers, sizes and brushes are those of the CheckBox template in Controls.Toggles.xaml (box, outer
/// highlight, inner shadow, the check mark <c>JetPack.Geometry.Check</c> in a 9px square, the 7×2 bar for
/// null), so the cell looks like the CheckBox it turns into for editing. The brushes are resource
/// references, so accent changes reach the cells.
/// </remarks>
public sealed class GridCheckBox : FrameworkElement
{
    private const double Size = 13;

    private static readonly DependencyProperty OuterHighlightProperty = Register("OuterHighlight");
    private static readonly DependencyProperty BoxBackgroundProperty = Register("BoxBackground");
    private static readonly DependencyProperty BoxBorderProperty = Register("BoxBorder");
    private static readonly DependencyProperty InnerShadowProperty = Register("InnerShadow");
    private static readonly DependencyProperty GlyphProperty = Register("Glyph");
    private static readonly DependencyProperty CheckProperty = DependencyProperty.Register(
        "Check", typeof(Geometry), typeof(GridCheckBox), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    [ThreadStatic]
    private static (Geometry Source, double Scale, Geometry Mark)? lastMark;

    private bool? isChecked = false;

    static GridCheckBox()
    {
        SnapsToDevicePixelsProperty.OverrideMetadata(typeof(GridCheckBox), new FrameworkPropertyMetadata(true));
        HorizontalAlignmentProperty.OverrideMetadata(typeof(GridCheckBox), new FrameworkPropertyMetadata(HorizontalAlignment.Center));
        VerticalAlignmentProperty.OverrideMetadata(typeof(GridCheckBox), new FrameworkPropertyMetadata(VerticalAlignment.Center));
        FocusableProperty.OverrideMetadata(typeof(GridCheckBox), new FrameworkPropertyMetadata(false));
        IsEnabledProperty.OverrideMetadata(typeof(GridCheckBox), new UIPropertyMetadata(true, (d, _) => ((GridCheckBox)d).OnIsEnabledChanged()));
    }

    public GridCheckBox()
    {
        SetResourceReference(OuterHighlightProperty, "JetPack.Brushes.Control.OuterHighlight");
        SetResourceReference(BoxBackgroundProperty, "JetPack.Brushes.CheckBox.Background");
        SetResourceReference(BoxBorderProperty, "JetPack.Brushes.CheckBox.Border");
        SetResourceReference(InnerShadowProperty, "JetPack.Brushes.CheckBox.InnerShadow");
        SetResourceReference(GlyphProperty, "JetPack.Brushes.Accent.Highlight");
        SetResourceReference(CheckProperty, "JetPack.Geometry.Check");
    }

    private static DependencyProperty Register(string name) => DependencyProperty.Register(
        name, typeof(Brush), typeof(GridCheckBox), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>The value shown: true draws the check mark, null the bar of an undecided CheckBox.</summary>
    public bool? IsChecked
    {
        get => isChecked;
        set
        {
            if (isChecked != value)
            {
                isChecked = value;
                InvalidateVisual();
            }
        }
    }

    // The disabled look of the CheckBox template.
    private void OnIsEnabledChanged()
    {
        if (IsEnabled)
        {
            SetResourceReference(BoxBackgroundProperty, "JetPack.Brushes.CheckBox.Background");
            SetResourceReference(BoxBorderProperty, "JetPack.Brushes.CheckBox.Border");
            SetResourceReference(GlyphProperty, "JetPack.Brushes.Accent.Highlight");
        }
        else
        {
            SetResourceReference(BoxBackgroundProperty, "JetPack.Brushes.Toggle.Disabled");
            SetResourceReference(BoxBorderProperty, "JetPack.Brushes.Toggle.Disabled.Border");
            SetResourceReference(GlyphProperty, "JetPack.Brushes.Toggle.Disabled.Glyph");
        }
    }

    protected override Size MeasureOverride(Size availableSize) => new(Size, Size);

    protected override Size ArrangeOverride(Size finalSize) => new(Size, Size);

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi) => InvalidateVisual();

    protected override void OnRender(DrawingContext dc)
    {
        // Each layer is drawn where layout puts the template's element, with the snapping guidelines a
        // SnapsToDevicePixels element gets at its edges (they decide the pixels where the offsets are not whole
        // device pixels). With layout rounding, sizes and offsets are rounded to device pixels, margins only
        // within the offset.
        var dpi = VisualTreeHelper.GetDpi(this);
        var rounding = UseLayoutRounding;
        double X(double value) => rounding ? Math.Round(value * dpi.DpiScaleX) / dpi.DpiScaleX : value;
        double Y(double value) => rounding ? Math.Round(value * dpi.DpiScaleY) / dpi.DpiScaleY : value;
        double width = X(Size), height = Y(Size);
        var box = Edges(width, height);
        // Border rounds its thickness as well.
        double lineX = X(1), lineY = Y(1);

        // The template's root Grid.
        dc.PushGuidelineSet(box);
        // Outer highlight: the right and bottom edge, one pixel outside the box (Margin 1,1,-1,-1, BorderThickness 0,0,1,1).
        if (GetValue(OuterHighlightProperty) is Brush highlight)
        {
            Push(dc, X(1), Y(1), box);
            dc.DrawRectangle(highlight, null, new Rect(width - lineX, 0, lineX, height));
            dc.DrawRectangle(highlight, null, new Rect(0, height - lineY, width - lineX, lineY));
            Pop(dc);
        }
        // The box: a one-pixel border, drawn with a pen as Border draws a uniform one, and the background inside it.
        dc.PushGuidelineSet(box);
        if (GetValue(BoxBorderProperty) is Brush border)
        {
            dc.DrawRectangle(null, new Pen(border, lineX), new Rect(lineX / 2, lineX / 2, width - lineX, height - lineX));
        }
        if (GetValue(BoxBackgroundProperty) is Brush background)
        {
            dc.DrawRectangle(background, null, new Rect(lineX, lineY, width - 2 * lineX, height - 2 * lineY));
        }
        dc.Pop();
        // Inner shadow: the top and left edge inside the border (Margin 1, BorderThickness 1,1,0,0).
        if (GetValue(InnerShadowProperty) is Brush shadow)
        {
            double innerWidth = X(width - 2), innerHeight = Y(height - 2);
            Push(dc, X(1), Y(1), Edges(innerWidth, innerHeight));
            dc.DrawRectangle(shadow, null, new Rect(0, 0, innerWidth, lineY));
            dc.DrawRectangle(shadow, null, new Rect(0, lineY, lineX, innerHeight - lineY));
            Pop(dc);
        }

        if (isChecked != false && GetValue(GlyphProperty) is Brush glyph)
        {
            if (isChecked == null)
            {
                // The 7×2 Rectangle, centred.
                double barWidth = X(7), barHeight = Y(2);
                Push(dc, X((width - barWidth) / 2), Y((height - barHeight) / 2), Edges(barWidth, barHeight));
                dc.DrawRectangle(glyph, null, new Rect(0, 0, barWidth, barHeight));
                Pop(dc);
            }
            else if (GetValue(CheckProperty) is Geometry geometry && geometry.Bounds is { IsEmpty: false, Width: > 0, Height: > 0 } bounds)
            {
                // The Path: Margin 2, Stretch="Uniform" in the room left inside, centred in it.
                double roomX = width - 4, roomY = height - 4;
                var scale = Math.Min(roomX / bounds.Width, roomY / bounds.Height);
                double markWidth = X(bounds.Width * scale), markHeight = Y(bounds.Height * scale);
                Push(dc, X(2 + (roomX - markWidth) / 2), Y(2 + (roomY - markHeight) / 2), Edges(markWidth, markHeight));
                dc.DrawGeometry(glyph, null, Mark(geometry, scale));
                Pop(dc);
            }
        }
        dc.Pop();
    }

    private static GuidelineSet Edges(double width, double height) => new([0, width], [0, height]);

    private static void Push(DrawingContext dc, double left, double top, GuidelineSet edges)
    {
        dc.PushTransform(new TranslateTransform(left, top));
        dc.PushGuidelineSet(edges);
    }

    private static void Pop(DrawingContext dc)
    {
        dc.Pop();
        dc.Pop();
    }

    // The check mark stretched from (0, 0), shared by every cell at the same scale.
    private static Geometry Mark(Geometry geometry, double scale)
    {
        if (lastMark is { } last && ReferenceEquals(last.Source, geometry) && last.Scale == scale)
        {
            return last.Mark;
        }
        var bounds = geometry.Bounds;
        var matrix = Matrix.Identity;
        matrix.ScaleAt(scale, scale, bounds.X, bounds.Y);
        matrix.Translate(-bounds.X, -bounds.Y);
        var stretched = geometry.Clone();
        stretched.Transform = new MatrixTransform(matrix);
        stretched.Freeze();
        lastMark = (geometry, scale, stretched);
        return stretched;
    }
}
