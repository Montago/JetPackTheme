using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JetPack.Wpf.Demo.Views;

public partial class PalettePage : UserControl
{
    public PalettePage()
    {
        InitializeComponent();
        Fill(AccentSwatches, "Accent.Selection", "Accent", "Accent.MouseOver", "Accent.Pressed", "Accent.Highlight", "Accent.Solid", "Accent.Link", "Accent.Button.Normal");
        Fill(NeutralSwatches, "Control.Background", "Control.Background.MouseOver", "Control.Background.Pressed", "Static.Background", "Input.Background",
            "Control.Border", "Control.Foreground", "Track", "Panel.Background", "Panel.Border", "DataGrid.Header", "Disabled", "Error", "ScrollBar.Track", "Window.Background");
        Fill(OtherSwatches, "Button.Green.Normal", "Button.Orange.Normal", "Button.Blue.Normal", "Chart.1", "Chart.2", "Chart.3", "Chart.4", "Chart.5", "Chart.6");
    }

    private static void Fill(Panel panel, params string[] names)
    {
        foreach (var name in names)
        {
            var key = "JetPack.Brushes." + name;
            var chip = new Border { Height = 44, BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0, 0, 0)), BorderThickness = new Thickness(0, 0, 0, 1) };
            chip.SetResourceReference(Border.BackgroundProperty, key);

            var meta = new StackPanel { Margin = new Thickness(8, 6, 8, 8) };
            meta.Children.Add(new TextBlock { Text = name, FontWeight = FontWeights.Bold, FontSize = 12 });
            var code = new TextBlock { Text = key, FontSize = 10.5, TextWrapping = TextWrapping.Wrap };
            code.SetResourceReference(TextBlock.FontFamilyProperty, "JetPack.Fonts.Family.Mono");
            code.SetResourceReference(TextBlock.ForegroundProperty, "JetPack.Brushes.Text.Soft");
            meta.Children.Add(code);

            var card = new StackPanel();
            card.Children.Add(chip);
            card.Children.Add(meta);
            panel.Children.Add(new Border
            {
                Width = 200,
                Margin = new Thickness(0, 0, 12, 12),
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(Color.FromArgb(0x1F, 0, 0, 0)),
                Child = card,
            });
        }
    }
}
