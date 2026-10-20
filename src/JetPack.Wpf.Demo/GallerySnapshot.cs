using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using JetPack.Wpf.Controls;
using JetPack.Wpf.Demo.Views;

namespace JetPack.Wpf.Demo;

/// <summary>
/// Renders every sample of <see cref="GalleryPage"/> to its own PNG on the window texture, with the
/// drop-downs, menus and tool tips marked <c>Demo.Open</c> drawn where they would appear.
/// </summary>
internal static class GallerySnapshot
{
    private const double Margin = 16;

    public static async Task SaveAsync(string folder)
    {
        Directory.CreateDirectory(folder);
        var gallery = new GalleryPage();
        var window = new JetPackWindow
        {
            Content = gallery,
            ShowActivated = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -20000,
            Top = 0,
            Width = 1280,
            Height = 900,
        };
        window.Show();

        try
        {
            var samples = gallery.Samples.ToList();
            foreach (var sample in samples)
            {
                foreach (var other in samples)
                {
                    other.Visibility = other == sample ? Visibility.Visible : Visibility.Collapsed;
                }
                await IdleAsync(window, 150);

                var opened = new List<Popup>();
                var menus = new List<FrameworkElement>();
                foreach (var element in Descendants(sample).OfType<FrameworkElement>().Where(Demo.GetOpen).ToList())
                {
                    Open(element, menus);
                }
                await IdleAsync(window, 450);

                foreach (var popup in Descendants(sample).OfType<Popup>().Where(p => p.IsOpen && p.Child != null))
                {
                    opened.Add(popup);
                }

                var parts = new List<(Visual Visual, Rect Bounds)>();
                foreach (var popup in opened)
                {
                    var target = popup.PlacementTarget ?? VisualTreeHelper.GetParent(popup) as UIElement;
                    AddPopup(parts, sample, popup.Child, target, popup.Placement, popup.HorizontalOffset, popup.VerticalOffset);
                }
                foreach (var menu in menus)
                {
                    var (target, placement, h, v) = menu switch
                    {
                        ContextMenu c => (c.PlacementTarget, c.Placement, c.HorizontalOffset, c.VerticalOffset),
                        ToolTip t => (t.PlacementTarget, t.Placement, t.HorizontalOffset, t.VerticalOffset),
                        _ => (null, PlacementMode.Bottom, 0d, 0d),
                    };
                    AddPopup(parts, sample, menu, target, placement, h, v);
                }

                Save(window, sample, parts, Path.Combine(folder, $"{sample.Tag}.png"));

                foreach (var popup in opened)
                {
                    popup.IsOpen = false;
                }
                foreach (var menu in menus)
                {
                    menu.SetValue(menu is ToolTip ? ToolTip.IsOpenProperty : ContextMenu.IsOpenProperty, false);
                }
            }
        }
        finally
        {
            window.Close();
        }
    }

    private static void Open(FrameworkElement element, List<FrameworkElement> menus)
    {
        // The validation call-out normally opens while the input has keyboard focus.
        if (Validation.GetHasError(element))
        {
            var adorners = AdornerLayer.GetAdornerLayer(element)?.GetAdorners(element) ?? Array.Empty<Adorner>();
            foreach (var popup in adorners.SelectMany(Descendants).OfType<Popup>())
            {
                popup.IsOpen = true;
            }
            return;
        }

        switch (element)
        {
            case ComboBox combo:
                combo.IsDropDownOpen = true;
                break;
            case DatePicker picker:
                picker.IsDropDownOpen = true;
                break;
            case TimePicker picker:
                picker.IsDropDownOpen = true;
                break;
            case ColorPicker picker:
                picker.IsDropDownOpen = true;
                break;
            case MenuItem item:
                // A top-level submenu needs mouse capture, which a window off the screen cannot get,
                // so show the popup directly and highlight the header as an open menu would.
                ForceOpen(item);
                typeof(MenuItem).GetProperty(nameof(MenuItem.IsHighlighted))!.SetValue(item, true);
                break;
            case AutoSuggestBox suggest:
                // Fills the suggestion list; the list itself only opens while the box has focus.
                suggest.Text = "fa";
                ForceOpen(suggest);
                break;
            case SplitButton { Menu: { } menu }:
                OpenMenu(element, menu, PlacementMode.Bottom, 0, 2, menus);
                break;
            case DropDownButton { Menu: { } menu }:
                OpenMenu(element, menu, PlacementMode.Bottom, 0, 2, menus);
                break;
            case { ContextMenu: { } menu }:
                OpenMenu(element, menu, PlacementMode.Relative, 130, 40, menus);
                break;
            case { ToolTip: { } tip }:
                var toolTip = tip as ToolTip ?? new ToolTip { Content = tip };
                element.ToolTip = toolTip;
                toolTip.PlacementTarget = element;
                toolTip.Placement = PlacementMode.Bottom;
                toolTip.VerticalOffset = 4;
                toolTip.IsOpen = true;
                menus.Add(toolTip);
                break;
        }

    }

    private static void ForceOpen(Control control)
    {
        // A local binding overrides the template's IsOpen binding without writing back to the control.
        if (control.Template?.FindName("PART_Popup", control) is Popup popup)
        {
            BindingOperations.SetBinding(popup, Popup.IsOpenProperty, new Binding { Source = true, Mode = BindingMode.OneWay });
        }
    }

    private static void OpenMenu(FrameworkElement owner, ContextMenu menu, PlacementMode placement, double h, double v, List<FrameworkElement> menus)
    {
        menu.PlacementTarget = owner;
        menu.Placement = placement;
        menu.HorizontalOffset = h;
        menu.VerticalOffset = v;
        if (placement == PlacementMode.Bottom)
        {
            menu.MinWidth = owner.ActualWidth;
        }
        menu.IsOpen = true;
        menus.Add(menu);
    }

    // Popups live in their own window, which is clamped to the screen; draw the popup's root where
    // its placement would put it next to the target instead.
    private static void AddPopup(List<(Visual, Rect)> parts, FrameworkElement sample, UIElement child, UIElement? target,
        PlacementMode placement, double h, double v)
    {
        if (target == null || PresentationSource.FromVisual(child)?.RootVisual is not FrameworkElement root)
        {
            return;
        }
        var size = target.RenderSize;
        var anchor = placement switch
        {
            PlacementMode.Right => new Point(size.Width + h, v),
            PlacementMode.Top => new Point(h, -root.ActualHeight + v),
            PlacementMode.Relative or PlacementMode.RelativePoint => new Point(h, v),
            _ => new Point(h, size.Height + v),
        };
        var at = target.TranslatePoint(anchor, sample);
        parts.Add((root, new Rect(at, new Size(root.ActualWidth, root.ActualHeight))));
    }

    private static void Save(Window window, FrameworkElement sample, List<(Visual Visual, Rect Bounds)> parts, string file)
    {
        var bounds = new Rect(0, 0, sample.ActualWidth, sample.ActualHeight);
        foreach (var part in parts)
        {
            var padded = part.Bounds;
            padded.Inflate(Margin, Margin);
            bounds.Union(padded);
        }
        bounds = new Rect(Math.Floor(bounds.X), Math.Floor(bounds.Y), Math.Ceiling(bounds.Width), Math.Ceiling(bounds.Height));

        var drawing = new DrawingVisual();
        using (var dc = drawing.RenderOpen())
        {
            dc.PushTransform(new TranslateTransform(-bounds.X, -bounds.Y));
            dc.DrawRectangle((Brush)window.FindResource("JetPack.Brushes.Window.Ground"), null, bounds);
            dc.DrawRectangle((Brush)window.FindResource("JetPack.Brushes.Window.Background"), null, bounds);
            Draw(dc, sample, new Rect(0, 0, sample.ActualWidth, sample.ActualHeight));
            foreach (var part in parts)
            {
                Draw(dc, part.Visual, part.Bounds);
            }
        }

        var bitmap = new RenderTargetBitmap((int)bounds.Width, (int)bounds.Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(drawing);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(file);
        encoder.Save(stream);
    }

    private static void Draw(DrawingContext dc, Visual visual, Rect at)
    {
        var box = new Rect(at.Size);
        dc.DrawRectangle(new VisualBrush(visual)
        {
            Stretch = Stretch.None,
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top,
            ViewboxUnits = BrushMappingMode.Absolute,
            Viewbox = box,
            ViewportUnits = BrushMappingMode.Absolute,
            Viewport = at,
        }, null, at);
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

    private static async Task IdleAsync(Window window, int delay)
    {
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        await Task.Delay(delay);
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
    }
}
