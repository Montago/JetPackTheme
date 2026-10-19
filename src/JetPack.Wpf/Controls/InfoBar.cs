using System.Windows;
using System.Windows.Controls;

namespace JetPack.Wpf.Controls;

public enum InfoBarSeverity
{
    Info,
    Success,
    Warning,
    Error,
}

/// <summary>Inline message bar: light strip with a coloured severity icon and an optional close button.</summary>
[TemplatePart(Name = "PART_Close", Type = typeof(Button))]
public class InfoBar : ContentControl
{
    static InfoBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(InfoBar), new FrameworkPropertyMetadata(typeof(InfoBar)));
    }

    public static readonly RoutedEvent ClosedEvent = EventManager.RegisterRoutedEvent(
        nameof(Closed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(InfoBar));

    public event RoutedEventHandler Closed
    {
        add => AddHandler(ClosedEvent, value);
        remove => RemoveHandler(ClosedEvent, value);
    }

    public static readonly DependencyProperty SeverityProperty = DependencyProperty.Register(
        nameof(Severity), typeof(InfoBarSeverity), typeof(InfoBar), new PropertyMetadata(InfoBarSeverity.Info));

    public InfoBarSeverity Severity
    {
        get => (InfoBarSeverity)GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(InfoBar), new PropertyMetadata(null));

    public string? Title
    {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(InfoBar), new PropertyMetadata(null));

    public string? Message
    {
        get => (string?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(InfoBar), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public static readonly DependencyProperty IsClosableProperty = DependencyProperty.Register(
        nameof(IsClosable), typeof(bool), typeof(InfoBar), new PropertyMetadata(true));

    public bool IsClosable
    {
        get => (bool)GetValue(IsClosableProperty);
        set => SetValue(IsClosableProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (GetTemplateChild("PART_Close") is Button close)
        {
            close.Click += (_, _) =>
            {
                IsOpen = false;
                RaiseEvent(new RoutedEventArgs(ClosedEvent, this));
            };
        }
    }
}
