using System.Windows;
using System.Windows.Controls;

namespace JetPack.Wpf.Controls;

/// <summary>
/// Vertical stack where the expanded <see cref="Expander"/> takes all the
/// height the closed ones leave, so their headers sit at the bottom (as in
/// the Toolkit Accordion). With unlimited height it behaves like a StackPanel.
/// </summary>
public class AccordionPanel : Panel
{
    private static bool IsFill(UIElement child) => child is Expander { IsExpanded: true };

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0.0;
        var fixedHeight = 0.0;
        UIElement? fill = null;

        foreach (UIElement child in InternalChildren)
        {
            if (fill == null && IsFill(child))
            {
                fill = child;
                continue;
            }
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            width = Math.Max(width, child.DesiredSize.Width);
            fixedHeight += child.DesiredSize.Height;
        }

        if (fill == null)
        {
            return new Size(width, fixedHeight);
        }

        var rest = double.IsInfinity(availableSize.Height) ? double.PositiveInfinity : Math.Max(0, availableSize.Height - fixedHeight);
        fill.Measure(new Size(availableSize.Width, rest));
        width = Math.Max(width, fill.DesiredSize.Width);
        return new Size(width, fixedHeight + fill.DesiredSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        UIElement? fill = null;
        var fixedHeight = 0.0;
        foreach (UIElement child in InternalChildren)
        {
            if (fill == null && IsFill(child))
            {
                fill = child;
            }
            else
            {
                fixedHeight += child.DesiredSize.Height;
            }
        }

        var y = 0.0;
        foreach (UIElement child in InternalChildren)
        {
            var height = ReferenceEquals(child, fill)
                ? Math.Max(child.DesiredSize.Height, finalSize.Height - fixedHeight)
                : child.DesiredSize.Height;
            child.Arrange(new Rect(0, y, finalSize.Width, height));
            y += height;
        }
        return finalSize;
    }
}
