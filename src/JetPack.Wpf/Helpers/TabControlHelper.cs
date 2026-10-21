using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace JetPack.Wpf.Helpers;

/// <summary>How the notch of the navigation and strip tabs moves to a newly selected tab.</summary>
public enum TabSelectionAnimation
{
    /// <summary>The notch jumps straight to the new tab.</summary>
    None,

    /// <summary>The notch slides along the bar and settles with a small overshoot.</summary>
    Slide,

    /// <summary>The notch pulls up into the bar and drops down under the new tab.</summary>
    Drop,
}

/// <summary>How the page changes when another tab is selected.</summary>
public enum TabContentTransition
{
    /// <summary>The new page replaces the old one at once.</summary>
    None,

    /// <summary>The old page fades out, then the new one fades in.</summary>
    Fade,
}

/// <summary>
/// Selection animations for TabControl. The navigation and strip templates mark their notch with
/// <c>IsSelectionIndicator</c>; this helper places it under the selected tab and moves it as set by
/// <c>SelectionAnimation</c>. <c>ContentTransition</c> works with any TabControl template that has a
/// <c>PART_SelectedContentHost</c>.
/// </summary>
public static class TabControlHelper
{
    public static readonly DependencyProperty SelectionAnimationProperty = DependencyProperty.RegisterAttached(
        "SelectionAnimation", typeof(TabSelectionAnimation), typeof(TabControlHelper),
        new FrameworkPropertyMetadata(TabSelectionAnimation.Slide, OnSelectionAnimationChanged));

    public static TabSelectionAnimation GetSelectionAnimation(DependencyObject o) => (TabSelectionAnimation)o.GetValue(SelectionAnimationProperty);
    public static void SetSelectionAnimation(DependencyObject o, TabSelectionAnimation value) => o.SetValue(SelectionAnimationProperty, value);

    public static readonly DependencyProperty ContentTransitionProperty = DependencyProperty.RegisterAttached(
        "ContentTransition", typeof(TabContentTransition), typeof(TabControlHelper),
        new FrameworkPropertyMetadata(TabContentTransition.None, OnContentTransitionChanged));

    public static TabContentTransition GetContentTransition(DependencyObject o) => (TabContentTransition)o.GetValue(ContentTransitionProperty);
    public static void SetContentTransition(DependencyObject o, TabContentTransition value) => o.SetValue(ContentTransitionProperty, value);

    /// <summary>Marks the element in a TabControl template that points at the selected tab (the notch).</summary>
    public static readonly DependencyProperty IsSelectionIndicatorProperty = DependencyProperty.RegisterAttached(
        "IsSelectionIndicator", typeof(bool), typeof(TabControlHelper), new FrameworkPropertyMetadata(false, OnIsSelectionIndicatorChanged));

    public static bool GetIsSelectionIndicator(DependencyObject o) => (bool)o.GetValue(IsSelectionIndicatorProperty);
    public static void SetIsSelectionIndicator(DependencyObject o, bool value) => o.SetValue(IsSelectionIndicatorProperty, value);

    private static readonly DependencyProperty TransitionsProperty = DependencyProperty.RegisterAttached(
        "Transitions", typeof(Transitions), typeof(TabControlHelper), new PropertyMetadata(null));

    private static void OnSelectionAnimationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Turning the animation off finishes a move that is under way.
        if (d is TabControl tabs && (TabSelectionAnimation)e.NewValue == TabSelectionAnimation.None)
        {
            (tabs.GetValue(TransitionsProperty) as Transitions)?.Snap();
        }
    }

    private static void OnContentTransitionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TabControl tabs)
        {
            return;
        }

        var transitions = Transitions.For(tabs);
        if ((TabContentTransition)e.NewValue == TabContentTransition.None)
        {
            transitions.EndFade();
        }
    }

    private static void OnIsSelectionIndicatorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement indicator)
        {
            return;
        }

        indicator.Loaded -= OnIndicatorLoaded;
        indicator.Unloaded -= OnIndicatorUnloaded;
        if ((bool)e.NewValue)
        {
            indicator.Loaded += OnIndicatorLoaded;
            indicator.Unloaded += OnIndicatorUnloaded;
            if (indicator.IsLoaded)
            {
                OnIndicatorLoaded(indicator, null);
            }
        }
    }

    private static void OnIndicatorLoaded(object sender, RoutedEventArgs? e)
    {
        var indicator = (FrameworkElement)sender;
        if (indicator.TemplatedParent is TabControl tabs)
        {
            Transitions.For(tabs).Attach(indicator);
        }
    }

    private static void OnIndicatorUnloaded(object sender, RoutedEventArgs e)
    {
        var indicator = (FrameworkElement)sender;
        if (indicator.TemplatedParent is TabControl tabs)
        {
            (tabs.GetValue(TransitionsProperty) as Transitions)?.Detach(indicator);
        }
    }

    /// <summary>The running animations of one TabControl.</summary>
    private sealed class Transitions
    {
        private static readonly Duration SlideDuration = TimeSpan.FromMilliseconds(320);
        private static readonly TimeSpan DropUp = TimeSpan.FromMilliseconds(120);
        private static readonly TimeSpan DropDown = TimeSpan.FromMilliseconds(380);
        private static readonly Duration FadeOut = TimeSpan.FromMilliseconds(140);
        private static readonly TimeSpan FadeInStart = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan FadeInEnd = TimeSpan.FromMilliseconds(300);

        private readonly TabControl tabs;
        private FrameworkElement? indicator;
        private TranslateTransform? offset;
        private bool moving;
        private int moveId;
        private PageSnapshot? oldPage;
        private PageSnapshot? snapshot;
        private UIElement? fadingHost;

        private Transitions(TabControl tabs)
        {
            this.tabs = tabs;
            tabs.SelectionChanged += OnSelectionChanged;
            tabs.AddHandler(Selector.UnselectedEvent, new RoutedEventHandler(OnUnselected), handledEventsToo: true);
        }

        public static Transitions For(TabControl tabs)
        {
            if (tabs.GetValue(TransitionsProperty) is not Transitions transitions)
            {
                transitions = new Transitions(tabs);
                tabs.SetValue(TransitionsProperty, transitions);
            }
            return transitions;
        }

        public void Attach(FrameworkElement element)
        {
            if (indicator != null)
            {
                Detach(indicator);
            }

            indicator = element;
            indicator.RenderTransform = offset = new TranslateTransform();
            indicator.LayoutUpdated += OnLayoutUpdated;
            Snap();
        }

        public void Detach(FrameworkElement element)
        {
            if (indicator != element)
            {
                return;
            }

            indicator.LayoutUpdated -= OnLayoutUpdated;
            indicator = null;
            offset = null;
            moving = false;
        }

        /// <summary>Puts the indicator under the selected tab at once, ending any move.</summary>
        public void Snap()
        {
            moving = false;
            if (indicator is null || offset is null)
            {
                return;
            }

            offset.BeginAnimation(TranslateTransform.XProperty, null);
            offset.BeginAnimation(TranslateTransform.YProperty, null);
            offset.Y = 0;
            var x = TargetX();
            if (x is { } value)
            {
                offset.X = value;
            }
            indicator.Visibility = x is null ? Visibility.Hidden : Visibility.Visible;
        }

        /// <summary>Left position that centres the indicator under the selected tab, or null when there is none.</summary>
        private double? TargetX()
        {
            if (indicator is null || VisualTreeHelper.GetParent(indicator) is not UIElement lane)
            {
                return null;
            }

            var item = tabs.ItemContainerGenerator.ContainerFromItem(tabs.SelectedItem) as TabItem ?? tabs.SelectedItem as TabItem;
            if (item is not { IsVisible: true } || item.ActualWidth <= 0 || !tabs.IsAncestorOf(item))
            {
                return null;
            }

            return item.TranslatePoint(new Point(item.ActualWidth / 2, 0), lane).X - indicator.ActualWidth / 2;
        }

        // Follows tabs that change width or move (other font size, items added) while nothing animates.
        private void OnLayoutUpdated(object? sender, EventArgs e)
        {
            if (moving || indicator is null || offset is null)
            {
                return;
            }

            var x = TargetX();
            var shown = indicator.Visibility == Visibility.Visible;
            if (x is null ? shown : !shown || Math.Abs(offset.X - x.Value) > 0.5)
            {
                Snap();
            }
        }

        // TabControl swaps SelectedContent before it raises SelectionChanged, but after the old tab is
        // unselected: that is the last moment the content host still shows the old page.
        private void OnUnselected(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is TabItem item && ItemsControl.ItemsControlFromItemContainer(item) == tabs
                && GetContentTransition(tabs) == TabContentTransition.Fade && tabs.IsLoaded)
            {
                EndFade();
                oldPage = CapturePage();
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ignore selection changes bubbling up from lists and combo boxes on the page.
            if (e.OriginalSource != tabs)
            {
                return;
            }

            Move();
            if (oldPage is { } page)
            {
                oldPage = null;
                Fade(page);
            }
        }

        private void Move()
        {
            var animation = GetSelectionAnimation(tabs);
            if (indicator is null || offset is null || indicator.Visibility != Visibility.Visible || animation == TabSelectionAnimation.None
                || TargetX() is not { } to)
            {
                Snap();
                return;
            }

            moving = true;
            var id = ++moveId;
            AnimationTimeline x, y;
            if (animation == TabSelectionAnimation.Drop)
            {
                // Up behind the bar (the lane clips its top edge), across, and down again with a little bounce.
                var hidden = -(indicator.ActualHeight + 4);
                x = new DoubleAnimationUsingKeyFrames { KeyFrames = { new DiscreteDoubleKeyFrame(to, DropUp) } };
                y = new DoubleAnimationUsingKeyFrames
                {
                    KeyFrames =
                    {
                        new EasingDoubleKeyFrame(hidden, DropUp, new QuadraticEase { EasingMode = EasingMode.EaseIn }),
                        new EasingDoubleKeyFrame(0, DropDown, new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.6 }),
                    },
                };
            }
            else
            {
                x = new DoubleAnimation(to, SlideDuration) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.35 } };
                y = new DoubleAnimation(0, TimeSpan.FromMilliseconds(120));
            }

            // The longer of the two ends the move.
            (animation == TabSelectionAnimation.Drop ? y : x).Completed += (_, _) =>
            {
                if (id == moveId)
                {
                    moving = false;
                }
            };
            offset.BeginAnimation(TranslateTransform.XProperty, x);
            offset.BeginAnimation(TranslateTransform.YProperty, y);
        }

        private PageSnapshot? CapturePage()
        {
            if (tabs.Template?.FindName("PART_SelectedContentHost", tabs) is not FrameworkElement host
                || host.ActualWidth < 1 || host.ActualHeight < 1 || AdornerLayer.GetAdornerLayer(host) is null)
            {
                return null;
            }

            var size = host.RenderSize;
            var dpi = VisualTreeHelper.GetDpi(host);
            var bitmap = new RenderTargetBitmap((int)Math.Ceiling(size.Width * dpi.DpiScaleX), (int)Math.Ceiling(size.Height * dpi.DpiScaleY),
                dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                // The brush draws the host at its offset in the parent, so the view box starts there.
                var brush = new VisualBrush(host) { Viewbox = new Rect((Point)VisualTreeHelper.GetOffset(host), size), ViewboxUnits = BrushMappingMode.Absolute };
                dc.DrawRectangle(brush, null, new Rect(size));
            }
            bitmap.Render(visual);
            bitmap.Freeze();
            return new PageSnapshot(host, bitmap);
        }

        /// <summary>Lays the picture of the old page over the new one and fades it out, then fades the new page in.</summary>
        private void Fade(PageSnapshot page)
        {
            var host = page.AdornedElement;
            if (AdornerLayer.GetAdornerLayer(host) is not { } layer)
            {
                return;
            }

            snapshot = page;
            fadingHost = host;
            layer.Add(page);

            var fadeOut = new DoubleAnimation(0, FadeOut) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            fadeOut.Completed += (_, _) =>
            {
                if (snapshot == page)
                {
                    layer.Remove(page);
                    snapshot = null;
                }
            };
            page.BeginAnimation(UIElement.OpacityProperty, fadeOut);

            var fadeIn = new DoubleAnimationUsingKeyFrames
            {
                FillBehavior = FillBehavior.Stop,
                KeyFrames =
                {
                    new DiscreteDoubleKeyFrame(0, TimeSpan.Zero),
                    new DiscreteDoubleKeyFrame(0, FadeInStart),
                    new EasingDoubleKeyFrame(1, FadeInEnd, new QuadraticEase { EasingMode = EasingMode.EaseOut }),
                },
            };
            host.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        /// <summary>Ends a page fade that is under way and shows the current page fully.</summary>
        public void EndFade()
        {
            if (snapshot != null)
            {
                AdornerLayer.GetAdornerLayer(snapshot.AdornedElement)?.Remove(snapshot);
                snapshot = null;
            }
            fadingHost?.BeginAnimation(UIElement.OpacityProperty, null);
            fadingHost = null;
            oldPage = null;
        }
    }

    /// <summary>A picture of the old page drawn over the content host while it fades out.</summary>
    private sealed class PageSnapshot : Adorner
    {
        private readonly ImageSource image;

        public PageSnapshot(UIElement host, ImageSource image) : base(host)
        {
            this.image = image;
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext dc) => dc.DrawImage(image, new Rect(AdornedElement.RenderSize));
    }
}
