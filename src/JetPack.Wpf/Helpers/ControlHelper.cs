using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace JetPack.Wpf.Helpers;

/// <summary>
/// Attached properties the JetPack templates read, so one template can serve
/// several looks (light, accent, green/orange/blue, dark inputs).
/// </summary>
public static class ControlHelper
{
    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.RegisterAttached(
        "CornerRadius", typeof(CornerRadius), typeof(ControlHelper),
        new FrameworkPropertyMetadata(new CornerRadius(2), FrameworkPropertyMetadataOptions.AffectsRender));

    public static CornerRadius GetCornerRadius(DependencyObject o) => (CornerRadius)o.GetValue(CornerRadiusProperty);
    public static void SetCornerRadius(DependencyObject o, CornerRadius value) => o.SetValue(CornerRadiusProperty, value);

    public static readonly DependencyProperty MouseOverBackgroundProperty = DependencyProperty.RegisterAttached(
        "MouseOverBackground", typeof(Brush), typeof(ControlHelper), new PropertyMetadata(null));

    public static Brush? GetMouseOverBackground(DependencyObject o) => (Brush?)o.GetValue(MouseOverBackgroundProperty);
    public static void SetMouseOverBackground(DependencyObject o, Brush? value) => o.SetValue(MouseOverBackgroundProperty, value);

    public static readonly DependencyProperty PressedBackgroundProperty = DependencyProperty.RegisterAttached(
        "PressedBackground", typeof(Brush), typeof(ControlHelper), new PropertyMetadata(null));

    public static Brush? GetPressedBackground(DependencyObject o) => (Brush?)o.GetValue(PressedBackgroundProperty);
    public static void SetPressedBackground(DependencyObject o, Brush? value) => o.SetValue(PressedBackgroundProperty, value);

    /// <summary>The 1px top/left highlight line on raised surfaces.</summary>
    public static readonly DependencyProperty HighlightBrushProperty = DependencyProperty.RegisterAttached(
        "HighlightBrush", typeof(Brush), typeof(ControlHelper), new PropertyMetadata(null));

    public static Brush? GetHighlightBrush(DependencyObject o) => (Brush?)o.GetValue(HighlightBrushProperty);
    public static void SetHighlightBrush(DependencyObject o, Brush? value) => o.SetValue(HighlightBrushProperty, value);

    /// <summary>Effect applied to the content (embossed or shadowed text).</summary>
    public static readonly DependencyProperty ContentEffectProperty = DependencyProperty.RegisterAttached(
        "ContentEffect", typeof(Effect), typeof(ControlHelper), new PropertyMetadata(null));

    public static Effect? GetContentEffect(DependencyObject o) => (Effect?)o.GetValue(ContentEffectProperty);
    public static void SetContentEffect(DependencyObject o, Effect? value) => o.SetValue(ContentEffectProperty, value);

    /// <summary>Dark top/left edge used by the dark input variants.</summary>
    public static readonly DependencyProperty EdgeBrushProperty = DependencyProperty.RegisterAttached(
        "EdgeBrush", typeof(Brush), typeof(ControlHelper), new PropertyMetadata(null));

    public static Brush? GetEdgeBrush(DependencyObject o) => (Brush?)o.GetValue(EdgeBrushProperty);
    public static void SetEdgeBrush(DependencyObject o, Brush? value) => o.SetValue(EdgeBrushProperty, value);

    public static readonly DependencyProperty InnerShadowDarkProperty = DependencyProperty.RegisterAttached(
        "InnerShadowDark", typeof(Brush), typeof(ControlHelper), new PropertyMetadata(null));

    public static Brush? GetInnerShadowDark(DependencyObject o) => (Brush?)o.GetValue(InnerShadowDarkProperty);
    public static void SetInnerShadowDark(DependencyObject o, Brush? value) => o.SetValue(InnerShadowDarkProperty, value);

    public static readonly DependencyProperty InnerShadowLightProperty = DependencyProperty.RegisterAttached(
        "InnerShadowLight", typeof(Brush), typeof(ControlHelper), new PropertyMetadata(null));

    public static Brush? GetInnerShadowLight(DependencyObject o) => (Brush?)o.GetValue(InnerShadowLightProperty);
    public static void SetInnerShadowLight(DependencyObject o, Brush? value) => o.SetValue(InnerShadowLightProperty, value);

    /// <summary>Overlay shown when an input is read-only.</summary>
    public static readonly DependencyProperty ReadOnlyBrushProperty = DependencyProperty.RegisterAttached(
        "ReadOnlyBrush", typeof(Brush), typeof(ControlHelper), new PropertyMetadata(null));

    public static Brush? GetReadOnlyBrush(DependencyObject o) => (Brush?)o.GetValue(ReadOnlyBrushProperty);
    public static void SetReadOnlyBrush(DependencyObject o, Brush? value) => o.SetValue(ReadOnlyBrushProperty, value);

    /// <summary>Overlay shown when an input is disabled.</summary>
    public static readonly DependencyProperty DisabledBrushProperty = DependencyProperty.RegisterAttached(
        "DisabledBrush", typeof(Brush), typeof(ControlHelper), new PropertyMetadata(null));

    public static Brush? GetDisabledBrush(DependencyObject o) => (Brush?)o.GetValue(DisabledBrushProperty);
    public static void SetDisabledBrush(DependencyObject o, Brush? value) => o.SetValue(DisabledBrushProperty, value);

    /// <summary>Style for the text box inside composite inputs (NumericUpDown, TimePicker, AutoSuggestBox).</summary>
    public static readonly DependencyProperty InputStyleProperty = DependencyProperty.RegisterAttached(
        "InputStyle", typeof(Style), typeof(ControlHelper), new PropertyMetadata(null));

    public static Style? GetInputStyle(DependencyObject o) => (Style?)o.GetValue(InputStyleProperty);
    public static void SetInputStyle(DependencyObject o, Style? value) => o.SetValue(InputStyleProperty, value);

    /// <summary>Text colour of a tab header (kept off the TabItem so page content does not inherit it).</summary>
    public static readonly DependencyProperty HeaderForegroundProperty = DependencyProperty.RegisterAttached(
        "HeaderForeground", typeof(Brush), typeof(ControlHelper), new PropertyMetadata(null));

    public static Brush? GetHeaderForeground(DependencyObject o) => (Brush?)o.GetValue(HeaderForegroundProperty);
    public static void SetHeaderForeground(DependencyObject o, Brush? value) => o.SetValue(HeaderForegroundProperty, value);
}
