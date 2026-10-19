using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace JetPack.Wpf.Controls;

/// <summary>Slider with two thumbs selecting a range (<see cref="LowerValue"/> to <see cref="UpperValue"/>).</summary>
[TemplatePart(Name = PartTrack, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartFill, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PartLower, Type = typeof(Thumb))]
[TemplatePart(Name = PartUpper, Type = typeof(Thumb))]
public class RangeSlider : Control
{
    private const string PartTrack = "PART_Track";
    private const string PartFill = "PART_Fill";
    private const string PartLower = "PART_LowerThumb";
    private const string PartUpper = "PART_UpperThumb";
    private const double ThumbHalf = 6;

    private FrameworkElement? track;
    private FrameworkElement? fill;
    private Thumb? lower;
    private Thumb? upper;

    static RangeSlider()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(RangeSlider), new FrameworkPropertyMetadata(typeof(RangeSlider)));
    }

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(RangeSlider), new PropertyMetadata(0d, OnLayoutChanged));

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(RangeSlider), new PropertyMetadata(100d, OnLayoutChanged));

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly DependencyProperty LowerValueProperty = DependencyProperty.Register(
        nameof(LowerValue), typeof(double), typeof(RangeSlider),
        new FrameworkPropertyMetadata(25d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnLayoutChanged, CoerceLower));

    public double LowerValue
    {
        get => (double)GetValue(LowerValueProperty);
        set => SetValue(LowerValueProperty, value);
    }

    public static readonly DependencyProperty UpperValueProperty = DependencyProperty.Register(
        nameof(UpperValue), typeof(double), typeof(RangeSlider),
        new FrameworkPropertyMetadata(75d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnLayoutChanged, CoerceUpper));

    public double UpperValue
    {
        get => (double)GetValue(UpperValueProperty);
        set => SetValue(UpperValueProperty, value);
    }

    public static readonly DependencyProperty SmallChangeProperty = DependencyProperty.Register(
        nameof(SmallChange), typeof(double), typeof(RangeSlider), new PropertyMetadata(1d));

    public double SmallChange
    {
        get => (double)GetValue(SmallChangeProperty);
        set => SetValue(SmallChangeProperty, value);
    }

    private static object CoerceLower(DependencyObject d, object v)
    {
        var r = (RangeSlider)d;
        return Math.Max(r.Minimum, Math.Min((double)v, r.UpperValue));
    }

    private static object CoerceUpper(DependencyObject d, object v)
    {
        var r = (RangeSlider)d;
        return Math.Min(r.Maximum, Math.Max((double)v, r.LowerValue));
    }

    private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var slider = (RangeSlider)d;
        // Values set in XAML arrive one at a time; re-coerce so LowerValue="2500" survives a default UpperValue of 75.
        if (e.Property != LowerValueProperty) slider.CoerceValue(LowerValueProperty);
        if (e.Property != UpperValueProperty) slider.CoerceValue(UpperValueProperty);
        slider.UpdateParts();
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        track = GetTemplateChild(PartTrack) as FrameworkElement;
        fill = GetTemplateChild(PartFill) as FrameworkElement;
        lower = GetTemplateChild(PartLower) as Thumb;
        upper = GetTemplateChild(PartUpper) as Thumb;

        if (track != null)
        {
            track.SizeChanged += (_, _) => UpdateParts();
        }
        if (lower != null)
        {
            lower.DragDelta += (_, e) => LowerValue += ToValueDelta(e.HorizontalChange);
        }
        if (upper != null)
        {
            upper.DragDelta += (_, e) => UpperValue += ToValueDelta(e.HorizontalChange);
        }
        UpdateParts();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        var step = e.Key switch { Key.Right or Key.Up => SmallChange, Key.Left or Key.Down => -SmallChange, _ => 0 };
        if (step == 0)
        {
            return;
        }
        if (ReferenceEquals(Keyboard.FocusedElement, lower))
        {
            LowerValue += step;
        }
        else
        {
            UpperValue += step;
        }
        e.Handled = true;
    }

    private double Usable => Math.Max(1, (track?.ActualWidth ?? 0) - 2 * ThumbHalf);

    private double ToValueDelta(double pixels) => pixels / Usable * (Maximum - Minimum);

    private double ToPixels(double value) => Maximum > Minimum ? (value - Minimum) / (Maximum - Minimum) * Usable : 0;

    private void UpdateParts()
    {
        if (track == null)
        {
            return;
        }
        var a = ToPixels(LowerValue);
        var b = ToPixels(UpperValue);
        if (lower != null) Canvas.SetLeft(lower, a);
        if (upper != null) Canvas.SetLeft(upper, b);
        if (fill != null)
        {
            Canvas.SetLeft(fill, a + ThumbHalf);
            fill.Width = Math.Max(0, b - a);
        }
    }
}
