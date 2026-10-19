using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace JetPack.Wpf.Controls;

public enum FlyoutPosition
{
    Right,
    Left,
}

/// <summary>Dark panel that slides in from the window edge, with a round back button and a header.</summary>
[TemplatePart(Name = "PART_Close", Type = typeof(Button))]
public class Flyout : HeaderedContentControl
{
    private readonly TranslateTransform slide = new();

    static Flyout()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Flyout), new FrameworkPropertyMetadata(typeof(Flyout)));
    }

    public Flyout()
    {
        RenderTransform = slide;
        Visibility = Visibility.Collapsed;
        Loaded += (_, _) => Apply(IsOpen, animate: false);
    }

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(Flyout),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((Flyout)d).Apply((bool)e.NewValue, animate: true)));

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
        nameof(Position), typeof(FlyoutPosition), typeof(Flyout), new PropertyMetadata(FlyoutPosition.Right));

    public FlyoutPosition Position
    {
        get => (FlyoutPosition)GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public static readonly DependencyProperty IsClosableProperty = DependencyProperty.Register(
        nameof(IsClosable), typeof(bool), typeof(Flyout), new PropertyMetadata(true));

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
            close.Click += (_, _) => IsOpen = false;
        }
    }

    private void Apply(bool open, bool animate)
    {
        var width = ActualWidth > 0 ? ActualWidth : (double.IsNaN(Width) ? 300 : Width);
        var hidden = Position == FlyoutPosition.Right ? width + 12 : -(width + 12);

        if (open)
        {
            Visibility = Visibility.Visible;
        }

        if (!animate || !IsLoaded || !SystemParameters.ClientAreaAnimation)
        {
            slide.BeginAnimation(TranslateTransform.XProperty, null);
            slide.X = open ? 0 : hidden;
            Visibility = open ? Visibility.Visible : Visibility.Collapsed;
            return;
        }

        var animation = new DoubleAnimation
        {
            From = open ? hidden : slide.X,
            To = open ? 0 : hidden,
            Duration = TimeSpan.FromMilliseconds(220),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        if (!open)
        {
            animation.Completed += (_, _) =>
            {
                if (!IsOpen)
                {
                    Visibility = Visibility.Collapsed;
                }
            };
        }
        slide.BeginAnimation(TranslateTransform.XProperty, animation);
    }
}
