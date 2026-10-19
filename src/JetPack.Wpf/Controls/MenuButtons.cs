using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace JetPack.Wpf.Controls;

/// <summary>Button that opens <see cref="Menu"/> under itself when clicked.</summary>
public class DropDownButton : Button
{
    static DropDownButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DropDownButton), new FrameworkPropertyMetadata(typeof(DropDownButton)));
    }

    public static readonly DependencyProperty MenuProperty = DependencyProperty.Register(
        nameof(Menu), typeof(ContextMenu), typeof(DropDownButton), new PropertyMetadata(null));

    public ContextMenu? Menu
    {
        get => (ContextMenu?)GetValue(MenuProperty);
        set => SetValue(MenuProperty, value);
    }

    protected override void OnClick()
    {
        base.OnClick();
        MenuButtonHelper.Open(this, Menu);
    }
}

/// <summary>
/// Button with a separate arrow part: the main part raises <c>Click</c>, the
/// arrow opens <see cref="Menu"/>.
/// </summary>
[TemplatePart(Name = PartArrow, Type = typeof(ToggleButton))]
public class SplitButton : Button
{
    private const string PartArrow = "PART_Arrow";
    private ToggleButton? arrow;

    static SplitButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SplitButton), new FrameworkPropertyMetadata(typeof(SplitButton)));
    }

    public static readonly DependencyProperty MenuProperty = DependencyProperty.Register(
        nameof(Menu), typeof(ContextMenu), typeof(SplitButton), new PropertyMetadata(null));

    public ContextMenu? Menu
    {
        get => (ContextMenu?)GetValue(MenuProperty);
        set => SetValue(MenuProperty, value);
    }

    public override void OnApplyTemplate()
    {
        if (arrow != null)
        {
            arrow.Checked -= OnArrowChecked;
        }
        base.OnApplyTemplate();
        arrow = GetTemplateChild(PartArrow) as ToggleButton;
        if (arrow != null)
        {
            arrow.Checked += OnArrowChecked;
        }
    }

    private void OnArrowChecked(object sender, RoutedEventArgs e)
    {
        if (Menu == null)
        {
            arrow!.IsChecked = false;
            return;
        }
        Menu.Closed -= OnMenuClosed;
        Menu.Closed += OnMenuClosed;
        MenuButtonHelper.Open(this, Menu);
    }

    private void OnMenuClosed(object sender, RoutedEventArgs e)
    {
        if (arrow != null)
        {
            arrow.IsChecked = false;
        }
    }
}

internal static class MenuButtonHelper
{
    public static void Open(FrameworkElement owner, ContextMenu? menu)
    {
        if (menu == null)
        {
            return;
        }
        menu.PlacementTarget = owner;
        menu.Placement = PlacementMode.Bottom;
        menu.VerticalOffset = 2;
        menu.MinWidth = owner.ActualWidth;
        menu.IsOpen = true;
    }
}
