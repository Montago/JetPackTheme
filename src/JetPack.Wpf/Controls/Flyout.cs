using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace JetPack.Wpf.Controls;

public enum FlyoutPosition
{
    Right,
    Left,
    Top,
    Bottom,
}

/// <summary>Dark panel that slides in from the window edge, with a round back button and a header.</summary>
[TemplatePart(Name = "PART_Close", Type = typeof(Button))]
public class Flyout : HeaderedContentControl
{
    private readonly TranslateTransform slide = new();
    private DispatcherTimer? autoClose;

    static Flyout()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Flyout), new FrameworkPropertyMetadata(typeof(Flyout)));
    }

    public Flyout()
    {
        RenderTransform = slide;
        Visibility = Visibility.Collapsed;
        Loaded += (_, _) => Apply(IsOpen, animate: false);
        Unloaded += (_, _) => autoClose?.Stop();
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

    public static readonly DependencyProperty IsAutoCloseEnabledProperty = DependencyProperty.Register(
        nameof(IsAutoCloseEnabled), typeof(bool), typeof(Flyout), new PropertyMetadata(false, (d, _) => ((Flyout)d).RestartAutoClose()));

    /// <summary>
    /// Closes the flyout <see cref="AutoCloseInterval"/> milliseconds after it opens, as in MahApps. The timer
    /// stops while the mouse is over the flyout and starts again when it leaves.
    /// </summary>
    public bool IsAutoCloseEnabled
    {
        get => (bool)GetValue(IsAutoCloseEnabledProperty);
        set => SetValue(IsAutoCloseEnabledProperty, value);
    }

    public static readonly DependencyProperty AutoCloseIntervalProperty = DependencyProperty.Register(
        nameof(AutoCloseInterval), typeof(long), typeof(Flyout), new PropertyMetadata(5000L, (d, _) => ((Flyout)d).RestartAutoClose()));

    /// <summary>Time in milliseconds before an auto-closing flyout closes.</summary>
    public long AutoCloseInterval
    {
        get => (long)GetValue(AutoCloseIntervalProperty);
        set => SetValue(AutoCloseIntervalProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (GetTemplateChild("PART_Close") is Button close)
        {
            close.Click += (_, _) => IsOpen = false;
        }
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        autoClose?.Stop();
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        RestartAutoClose();
    }

    private void RestartAutoClose()
    {
        autoClose?.Stop();
        if (!IsOpen || !IsAutoCloseEnabled || AutoCloseInterval <= 0 || IsMouseOver)
        {
            return;
        }

        if (autoClose == null)
        {
            autoClose = new DispatcherTimer();
            autoClose.Tick += (_, _) =>
            {
                autoClose.Stop();
                IsOpen = false;
            };
        }
        autoClose.Interval = TimeSpan.FromMilliseconds(AutoCloseInterval);
        autoClose.Start();
    }

    private void Apply(bool open, bool animate)
    {
        RestartAutoClose();

        var vertical = Position is FlyoutPosition.Top or FlyoutPosition.Bottom;
        var axis = vertical ? TranslateTransform.YProperty : TranslateTransform.XProperty;
        var otherAxis = vertical ? TranslateTransform.XProperty : TranslateTransform.YProperty;
        slide.BeginAnimation(otherAxis, null);
        slide.SetValue(otherAxis, 0.0);

        if (open && Visibility != Visibility.Visible)
        {
            Visibility = Visibility.Visible;
            if (IsLoaded)
            {
                // Measure now, so a flyout without a fixed size knows how far it has to travel.
                UpdateLayout();
            }
        }

        var hidden = HiddenOffset(vertical);
        if (!animate || !IsLoaded || !SystemParameters.ClientAreaAnimation)
        {
            slide.BeginAnimation(axis, null);
            slide.SetValue(axis, open ? 0.0 : hidden);
            Visibility = open ? Visibility.Visible : Visibility.Collapsed;
            return;
        }

        var animation = new DoubleAnimation
        {
            From = open ? hidden : (double)slide.GetValue(axis),
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
        slide.BeginAnimation(axis, animation);
    }

    /// <summary>Offset that puts the flyout (and its shadow) just outside its edge.</summary>
    private double HiddenOffset(bool vertical)
    {
        var size = vertical ? ActualHeight : ActualWidth;
        if (size <= 0)
        {
            var fixedSize = vertical ? Height : Width;
            size = double.IsNaN(fixedSize) ? (vertical ? 120 : 300) : fixedSize;
        }

        size += 12;
        return Position is FlyoutPosition.Right or FlyoutPosition.Bottom ? size : -size;
    }
}
