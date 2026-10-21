using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace JetPack.Wpf.Helpers;

/// <summary>
/// Auto-hiding scroll bars for the thin ScrollViewer style. While <c>AutoHide</c> is on, the
/// scroll viewer is marked active when it scrolls or the mouse moves over it, and inactive again
/// once nothing has happened for <c>AutoHideDelay</c>. The template fades its bars on
/// <c>IsScrollBarActive</c>.
/// </summary>
public static class ScrollViewerHelper
{
    public static readonly DependencyProperty AutoHideProperty = DependencyProperty.RegisterAttached(
        "AutoHide", typeof(bool), typeof(ScrollViewerHelper), new FrameworkPropertyMetadata(false, OnAutoHideChanged));

    public static bool GetAutoHide(DependencyObject o) => (bool)o.GetValue(AutoHideProperty);
    public static void SetAutoHide(DependencyObject o, bool value) => o.SetValue(AutoHideProperty, value);

    /// <summary>How long the bars stay after the last scroll or mouse move (default 1.5 seconds).</summary>
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
            sv.PreviewMouseMove += OnMouseMove;
            sv.MouseLeave += OnMouseLeave;
            sv.Unloaded += OnUnloaded;
            sv.SetValue(IsScrollBarActivePropertyKey, false);
        }
        else
        {
            sv.ScrollChanged -= OnScrollChanged;
            sv.PreviewMouseMove -= OnMouseMove;
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
            Activate((ScrollViewer)sender);
        }
    }

    private static void OnMouseMove(object sender, MouseEventArgs e) => Activate((ScrollViewer)sender);

    private static void OnMouseLeave(object sender, MouseEventArgs e) => Restart((ScrollViewer)sender);

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        var sv = (ScrollViewer)sender;
        (sv.GetValue(TimerProperty) as DispatcherTimer)?.Stop();
        sv.SetValue(IsScrollBarActivePropertyKey, false);
    }

    private static void Activate(ScrollViewer sv)
    {
        sv.SetValue(IsScrollBarActivePropertyKey, true);
        Restart(sv);
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
        // Keep the bars while the mouse is on one of them or a thumb is being dragged.
        if (Mouse.Captured is Thumb thumb && thumb.IsDescendantOf(sv) || IsOverBar(sv, "PART_VerticalScrollBar") || IsOverBar(sv, "PART_HorizontalScrollBar"))
        {
            return;
        }

        timer.Stop();
        sv.SetValue(IsScrollBarActivePropertyKey, false);
    }

    private static bool IsOverBar(ScrollViewer sv, string part) => sv.Template?.FindName(part, sv) is ScrollBar { IsMouseOver: true };
}
