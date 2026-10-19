using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace JetPack.Wpf.Demo;

/// <summary>Small attached helpers that keep the demo XAML short.</summary>
public static class Demo
{
    /// <summary>Vertical gap between the children of a Panel.</summary>
    public static readonly DependencyProperty SpacingProperty = DependencyProperty.RegisterAttached(
        "Spacing", typeof(double), typeof(Demo), new PropertyMetadata(0d, OnSpacingChanged));

    public static double GetSpacing(DependencyObject o) => (double)o.GetValue(SpacingProperty);
    public static void SetSpacing(DependencyObject o, double value) => o.SetValue(SpacingProperty, value);

    private static void OnSpacingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Panel panel)
        {
            panel.Loaded += (_, _) => ApplySpacing(panel);
        }
    }

    private static void ApplySpacing(Panel panel)
    {
        var gap = GetSpacing(panel);
        var horizontal = panel is StackPanel { Orientation: Orientation.Horizontal };
        var children = panel.Children.OfType<FrameworkElement>().ToList();
        for (var i = 0; i < children.Count - 1; i++)
        {
            var m = children[i].Margin;
            children[i].Margin = horizontal ? new Thickness(m.Left, m.Top, gap, m.Bottom) : new Thickness(m.Left, m.Top, m.Right, gap);
        }
    }

    /// <summary>Marks an element as invalid with the given message so the validation look shows.</summary>
    public static readonly DependencyProperty ErrorProperty = DependencyProperty.RegisterAttached(
        "Error", typeof(string), typeof(Demo), new PropertyMetadata(null, OnErrorChanged));

    public static string? GetError(DependencyObject o) => (string?)o.GetValue(ErrorProperty);
    public static void SetError(DependencyObject o, string? value) => o.SetValue(ErrorProperty, value);

    private static void OnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement fe || e.NewValue is not string message)
        {
            return;
        }
        fe.Loaded += (_, _) =>
        {
            var expression = BindingOperations.SetBinding(fe, FrameworkElement.TagProperty, new Binding { Source = message });
            Validation.MarkInvalid(expression, new ValidationError(new ExceptionValidationRule(), expression, message, null));
        };
    }
}

public sealed record Timesheet(string Employee, string Department, double Hours, int Week, string Status);
