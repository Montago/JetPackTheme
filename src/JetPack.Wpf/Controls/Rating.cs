using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace JetPack.Wpf.Controls;

/// <summary>Row of stars; filled stars use the accent highlight, empty ones the dark track colour.</summary>
[TemplatePart(Name = PartHost, Type = typeof(Panel))]
public class Rating : Control
{
    private const string PartHost = "PART_Host";
    private static readonly Geometry Star = Geometry.Parse("M9,1.5 L11.3,6.3 16.5,7 12.7,10.6 13.6,15.8 9,13.3 4.4,15.8 5.3,10.6 1.5,7 6.7,6.3 z");
    private Panel? host;
    private int hover;

    static Rating()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Rating), new FrameworkPropertyMetadata(typeof(Rating)));
        Star.Freeze();
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(int), typeof(Rating), new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((Rating)d).Refresh()));

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(int), typeof(Rating), new PropertyMetadata(5, (d, _) => ((Rating)d).Build()));

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly), typeof(bool), typeof(Rating), new PropertyMetadata(false));

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        host = GetTemplateChild(PartHost) as Panel;
        Build();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (IsReadOnly) return;
        if (e.Key is Key.Right or Key.Up) { Value = Math.Min(Maximum, Value + 1); e.Handled = true; }
        if (e.Key is Key.Left or Key.Down) { Value = Math.Max(0, Value - 1); e.Handled = true; }
    }

    private void Build()
    {
        if (host == null)
        {
            return;
        }
        host.Children.Clear();
        for (var i = 1; i <= Maximum; i++)
        {
            var index = i;
            var star = new Path { Data = Star, Width = 18, Height = 18, StrokeThickness = 1, Margin = new Thickness(0, 0, 2, 0), Cursor = Cursors.Hand };
            star.MouseEnter += (_, _) => { if (!IsReadOnly) { hover = index; Refresh(); } };
            star.MouseLeave += (_, _) => { hover = 0; Refresh(); };
            star.MouseLeftButtonUp += (_, _) => { if (!IsReadOnly) Value = Value == index ? 0 : index; };
            host.Children.Add(star);
        }
        Refresh();
    }

    private void Refresh()
    {
        if (host == null)
        {
            return;
        }
        var shown = hover > 0 ? hover : Value;
        for (var i = 0; i < host.Children.Count; i++)
        {
            if (host.Children[i] is not Path p) continue;
            var on = i < shown;
            p.SetResourceReference(Shape.FillProperty, on ? "JetPack.Brushes.Accent.Highlight" : "JetPack.Brushes.Track");
            p.SetResourceReference(Shape.StrokeProperty, on ? "JetPack.Brushes.Accent.Solid" : "JetPack.Brushes.CheckBox.Border");
        }
    }
}
