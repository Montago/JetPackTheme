using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace JetPack.Wpf.Helpers;

/// <summary>
/// Auto-hiding scroll bars for the thin ScrollViewer style (on by default there). While
/// <c>AutoHide</c> is on, the scroll viewer is marked active while the mouse is over it, and for
/// <c>AutoHideDelay</c> after it scrolls without the mouse (keyboard, code). The template fades its
/// bars in and out on <c>IsScrollBarActive</c>.
/// </summary>
public static class ScrollViewerHelper
{
    public static readonly DependencyProperty AutoHideProperty = DependencyProperty.RegisterAttached(
        "AutoHide", typeof(bool), typeof(ScrollViewerHelper), new FrameworkPropertyMetadata(false, OnAutoHideChanged));

    public static bool GetAutoHide(DependencyObject o) => (bool)o.GetValue(AutoHideProperty);
    public static void SetAutoHide(DependencyObject o, bool value) => o.SetValue(AutoHideProperty, value);

    /// <summary>How long the bars stay after scrolling while the mouse is elsewhere (default 1.5 seconds).</summary>
    public static readonly DependencyProperty AutoHideDelayProperty = DependencyProperty.RegisterAttached(
        "AutoHideDelay", typeof(TimeSpan), typeof(ScrollViewerHelper), new FrameworkPropertyMetadata(TimeSpan.FromSeconds(1.5)));

    public static TimeSpan GetAutoHideDelay(DependencyObject o) => (TimeSpan)o.GetValue(AutoHideDelayProperty);
    public static void SetAutoHideDelay(DependencyObject o, TimeSpan value) => o.SetValue(AutoHideDelayProperty, value);

    private static readonly DependencyPropertyKey IsScrollBarActivePropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "IsScrollBarActive", typeof(bool), typeof(ScrollViewerHelper), new FrameworkPropertyMetadata(true));

    /// <summary>True while the bars should show. Always true when <c>AutoHide</c> is off.</summary>
    public static readonly DependencyProperty IsScrollBarActiveProperty = IsScrollBarActivePropertyKey.DependencyProperty;

    public static bool GetIsScrollBarActive(DependencyObject o) => (bool)o.GetValue(IsScrollBarActiveProperty);

    private static readonly DependencyProperty TimerProperty = DependencyProperty.RegisterAttached(
        "Timer", typeof(DispatcherTimer), typeof(ScrollViewerHelper), new PropertyMetadata(null));

    private static void OnAutoHideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer sv)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            sv.ScrollChanged += OnScrollChanged;
            sv.MouseEnter += OnMouseEnter;
            sv.PreviewMouseMove += OnMouseEnter;
            sv.MouseLeave += OnMouseLeave;
            sv.Unloaded += OnUnloaded;
            sv.SetValue(IsScrollBarActivePropertyKey, sv.IsMouseOver);
        }
        else
        {
            sv.ScrollChanged -= OnScrollChanged;
            sv.MouseEnter -= OnMouseEnter;
            sv.PreviewMouseMove -= OnMouseEnter;
            sv.MouseLeave -= OnMouseLeave;
            sv.Unloaded -= OnUnloaded;
            (sv.GetValue(TimerProperty) as DispatcherTimer)?.Stop();
            sv.SetValue(IsScrollBarActivePropertyKey, true);
        }
    }

    private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // Only real scrolling; a change of extent or viewport (loading, resizing) leaves the bars hidden.
        if (e.OriginalSource == sender && (e.VerticalChange != 0 || e.HorizontalChange != 0))
        {
            var sv = (ScrollViewer)sender;
            sv.SetValue(IsScrollBarActivePropertyKey, true);
            Restart(sv);
        }
    }

    // The bars show for as long as the mouse is over the scroll viewer.
    private static void OnMouseEnter(object sender, MouseEventArgs e)
    {
        var sv = (ScrollViewer)sender;
        if (!GetIsScrollBarActive(sv))
        {
            sv.SetValue(IsScrollBarActivePropertyKey, true);
        }
    }

    private static void OnMouseLeave(object sender, MouseEventArgs e)
    {
        var sv = (ScrollViewer)sender;
        if (!IsDragging(sv))
        {
            (sv.GetValue(TimerProperty) as DispatcherTimer)?.Stop();
            sv.SetValue(IsScrollBarActivePropertyKey, false);
        }
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        var sv = (ScrollViewer)sender;
        (sv.GetValue(TimerProperty) as DispatcherTimer)?.Stop();
        sv.SetValue(IsScrollBarActivePropertyKey, false);
    }

    private static void Restart(ScrollViewer sv)
    {
        if (sv.GetValue(TimerProperty) is not DispatcherTimer timer)
        {
            timer = new DispatcherTimer(DispatcherPriority.Background, sv.Dispatcher);
            timer.Tick += (_, _) => OnTimeout(sv, timer);
            sv.SetValue(TimerProperty, timer);
        }

        timer.Stop();
        timer.Interval = GetAutoHideDelay(sv);
        timer.Start();
    }

    private static void OnTimeout(ScrollViewer sv, DispatcherTimer timer)
    {
        // A thumb being dragged keeps the bars; so does the mouse, until it leaves.
        if (IsDragging(sv))
        {
            return;
        }

        timer.Stop();
        if (!sv.IsMouseOver)
        {
            sv.SetValue(IsScrollBarActivePropertyKey, false);
        }
    }

    private static bool IsDragging(ScrollViewer sv) => Mouse.Captured is Thumb thumb && thumb.IsDescendantOf(sv);
}
