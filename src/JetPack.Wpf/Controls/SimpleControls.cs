using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace JetPack.Wpf.Controls;

/// <summary>On/off switch: a dark recessed track with a light sliding knob; accent when on.</summary>
public class ToggleSwitch : ToggleButton
{
    static ToggleSwitch()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ToggleSwitch), new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
    }
}

/// <summary>Circular busy indicator: an accent arc spinning over a dark recessed ring.</summary>
public class ProgressRing : Control
{
    static ProgressRing()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ProgressRing), new FrameworkPropertyMetadata(typeof(ProgressRing)));
    }

    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive), typeof(bool), typeof(ProgressRing), new PropertyMetadata(true));

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }
}

/// <summary>Puts a small orange count badge on the top-right corner of its content.</summary>
public class Badged : ContentControl
{
    static Badged()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Badged), new FrameworkPropertyMetadata(typeof(Badged)));
    }

    public static readonly DependencyProperty BadgeProperty = DependencyProperty.Register(
        nameof(Badge), typeof(object), typeof(Badged), new PropertyMetadata(null));

    /// <summary>Badge content; the badge is hidden when null.</summary>
    public object? Badge
    {
        get => GetValue(BadgeProperty);
        set => SetValue(BadgeProperty, value);
    }

    public static readonly DependencyProperty BadgeBackgroundProperty = DependencyProperty.Register(
        nameof(BadgeBackground), typeof(Brush), typeof(Badged), new PropertyMetadata(null));

    public Brush? BadgeBackground
    {
        get => (Brush?)GetValue(BadgeBackgroundProperty);
        set => SetValue(BadgeBackgroundProperty, value);
    }
}

/// <summary>Large square button with a count in the top-right and a title in the bottom-left.</summary>
public class Tile : Button
{
    static Tile()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Tile), new FrameworkPropertyMetadata(typeof(Tile)));
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(Tile), new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty CountProperty = DependencyProperty.Register(
        nameof(Count), typeof(object), typeof(Tile), new PropertyMetadata(null));

    public object? Count
    {
        get => GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }
}

/// <summary>Dims its content and shows a small "working" card on top while <see cref="IsBusy"/> is true.</summary>
public class BusyIndicator : ContentControl
{
    static BusyIndicator()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(BusyIndicator), new FrameworkPropertyMetadata(typeof(BusyIndicator)));
    }

    public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
        nameof(IsBusy), typeof(bool), typeof(BusyIndicator), new PropertyMetadata(false));

    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }

    public static readonly DependencyProperty BusyContentProperty = DependencyProperty.Register(
        nameof(BusyContent), typeof(object), typeof(BusyIndicator), new PropertyMetadata("Please wait…"));

    public object? BusyContent
    {
        get => GetValue(BusyContentProperty);
        set => SetValue(BusyContentProperty, value);
    }
}

/// <summary>Stack of expanders where opening one closes the others (the Toolkit Accordion).</summary>
public class Accordion : ItemsControl
{
    static Accordion()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Accordion), new FrameworkPropertyMetadata(typeof(Accordion)));
    }

    public Accordion()
    {
        AddHandler(Expander.ExpandedEvent, new RoutedEventHandler(OnItemExpanded));
    }

    protected override bool IsItemItsOwnContainerOverride(object item) => item is Expander;

    protected override DependencyObject GetContainerForItemOverride() => new Expander();

    private void OnItemExpanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Expander opened || ItemsControlFromItemContainer(opened) != this)
        {
            return;
        }

        for (var i = 0; i < Items.Count; i++)
        {
            if (ItemContainerGenerator.ContainerFromIndex(i) is Expander other && !ReferenceEquals(other, opened))
            {
                other.IsExpanded = false;
            }
        }
    }
}
