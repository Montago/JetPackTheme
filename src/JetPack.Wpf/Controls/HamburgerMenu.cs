using System.Windows;
using System.Windows.Controls;

namespace JetPack.Wpf.Controls;

/// <summary>
/// Navigation pane on the left (a list of <see cref="HamburgerMenuItem"/>) and
/// a content area on the right. The selected item gets the accent gradient.
/// </summary>
public class HamburgerMenu : ListBox
{
    static HamburgerMenu()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(HamburgerMenu), new FrameworkPropertyMetadata(typeof(HamburgerMenu)));
    }

    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content), typeof(object), typeof(HamburgerMenu), new PropertyMetadata(null));

    /// <summary>What the content area shows; usually bound to the selected item.</summary>
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public static readonly DependencyProperty ContentTemplateProperty = DependencyProperty.Register(
        nameof(ContentTemplate), typeof(DataTemplate), typeof(HamburgerMenu), new PropertyMetadata(null));

    public DataTemplate? ContentTemplate
    {
        get => (DataTemplate?)GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }

    public static readonly DependencyProperty IsPaneOpenProperty = DependencyProperty.Register(
        nameof(IsPaneOpen), typeof(bool), typeof(HamburgerMenu), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public bool IsPaneOpen
    {
        get => (bool)GetValue(IsPaneOpenProperty);
        set => SetValue(IsPaneOpenProperty, value);
    }

    public static readonly DependencyProperty PaneWidthProperty = DependencyProperty.Register(
        nameof(PaneWidth), typeof(double), typeof(HamburgerMenu), new PropertyMetadata(170d));

    public double PaneWidth
    {
        get => (double)GetValue(PaneWidthProperty);
        set => SetValue(PaneWidthProperty, value);
    }

    public static readonly DependencyProperty CompactPaneWidthProperty = DependencyProperty.Register(
        nameof(CompactPaneWidth), typeof(double), typeof(HamburgerMenu), new PropertyMetadata(40d));

    public double CompactPaneWidth
    {
        get => (double)GetValue(CompactPaneWidthProperty);
        set => SetValue(CompactPaneWidthProperty, value);
    }

    protected override bool IsItemItsOwnContainerOverride(object item) => item is HamburgerMenuItem;

    protected override DependencyObject GetContainerForItemOverride() => new HamburgerMenuItem();
}

public class HamburgerMenuItem : ListBoxItem
{
    static HamburgerMenuItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(HamburgerMenuItem), new FrameworkPropertyMetadata(typeof(HamburgerMenuItem)));
    }

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(object), typeof(HamburgerMenuItem), new PropertyMetadata(null));

    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
}
