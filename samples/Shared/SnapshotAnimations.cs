using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using JetPack.Wpf.Helpers;

namespace JetPack.Wpf.Samples;

/// <summary>
/// Snapshot support: stops the endless animations (indeterminate progress stripes, ProgressRing) at
/// their start, so the same page gives the same picture on every run and the documentation pictures
/// only change when the look changes. Finite animations (a ToggleSwitch knob) are left to finish;
/// the tab notch and page fade are switched off.
/// </summary>
internal static class SnapshotAnimations
{
    public static void Freeze(DependencyObject root)
    {
        // Tab notches and page fades jump to their end, and later tab changes no longer animate.
        foreach (var tabs in Descendants(root).OfType<TabControl>())
        {
            TabControlHelper.SetSelectionAnimation(tabs, TabSelectionAnimation.None);
            TabControlHelper.SetContentTransition(tabs, TabContentTransition.None);
        }

        foreach (var control in Descendants(root).OfType<Control>())
        {
            if (control.Template is not { } template)
            {
                continue;
            }
            var storyboards = template.Triggers
                .SelectMany(t => t.EnterActions.Concat(t.ExitActions))
                .OfType<BeginStoryboard>()
                .Select(b => b.Storyboard)
                .Where(s => s is { RepeatBehavior.HasDuration: false, RepeatBehavior.HasCount: false });
            foreach (var storyboard in storyboards)
            {
                // Does nothing (apart from a trace message) when the storyboard is not running here.
                storyboard.SeekAlignedToLastTick(control, TimeSpan.Zero, TimeSeekOrigin.BeginTime);
                storyboard.Pause(control);
            }
        }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var d in Descendants(child))
            {
                yield return d;
            }
        }
    }
}
