using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JetPack.Wpf.Controls;

/// <summary>
/// One of the JetPack icons (Material Design Icons), drawn in <see cref="Control.Foreground"/>, 16×16
/// by default: <c>&lt;jp:JetPackIcon Kind="Bell" /&gt;</c>. The set is listed in icons/icons.json;
/// <see cref="Data"/> gives the geometry for a Path of your own.
/// </summary>
public class JetPackIcon : Control
{
    private static readonly Dictionary<JetPackIconKind, Geometry> Cache = [];

    static JetPackIcon()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(JetPackIcon), new FrameworkPropertyMetadata(typeof(JetPackIcon)));
        FocusableProperty.OverrideMetadata(typeof(JetPackIcon), new FrameworkPropertyMetadata(false));
    }

    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(JetPackIconKind), typeof(JetPackIcon),
        new PropertyMetadata(JetPackIconKind.None, (d, e) => d.SetValue(DataPropertyKey, GeometryOf((JetPackIconKind)e.NewValue))));

    public JetPackIconKind Kind
    {
        get => (JetPackIconKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    private static readonly DependencyPropertyKey DataPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Data), typeof(Geometry), typeof(JetPackIcon), new PropertyMetadata(null));

    public static readonly DependencyProperty DataProperty = DataPropertyKey.DependencyProperty;

    /// <summary>The icon's path in a 24×24 box.</summary>
    public Geometry? Data => (Geometry?)GetValue(DataProperty);

    /// <summary>The frozen geometry of an icon in a 24×24 box, or null for None.</summary>
    public static Geometry? GeometryOf(JetPackIconKind kind)
    {
        if (kind == JetPackIconKind.None)
        {
            return null;
        }
        lock (Cache)
        {
            if (!Cache.TryGetValue(kind, out var geometry))
            {
                // F1: MDI is drawn for the nonzero fill rule; WPF's path markup defaults to even-odd
                geometry = Geometry.Parse("F1 " + JetPackIconData.Get(kind));
                geometry.Freeze();
                Cache[kind] = geometry;
            }
            return geometry;
        }
    }
}
