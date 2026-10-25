using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JetPack.Wpf.Controls;

namespace JetPack.Wpf.Demo.Views;

public partial class IconsPage : UserControl
{
    private readonly List<JetPackIcon> icons = [];
    private double size = 18;

    public IconsPage()
    {
        InitializeComponent();
        foreach (var kind in Enum.GetValues<JetPackIconKind>().Where(k => k != JetPackIconKind.None))
        {
            Icons.Children.Add(Card(kind));
        }
    }

    // One card per icon: on the navigation pane, selected, on a light button, then the name
    private Border Card(JetPackIconKind kind)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
        row.Children.Add(Swatch(kind, "JetPack.Brushes.Navigation.Inactive", "JetPack.Brushes.Tab.Inactive.Foreground"));
        row.Children.Add(Swatch(kind, "JetPack.Brushes.Accent.Selection", "JetPack.Brushes.Text"));
        var button = new Button { Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Focusable = false, Content = Icon(kind, "JetPack.Brushes.Control.Glyph.Dark") };
        button.SetResourceReference(StyleProperty, "JetPack.Styles.Button.Square");
        row.Children.Add(button);

        var name = new TextBlock { Text = kind.ToString(), FontSize = 11, Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Center };
        name.SetResourceReference(TextBlock.FontFamilyProperty, "JetPack.Fonts.Family.Mono");
        name.SetResourceReference(TextBlock.ForegroundProperty, "JetPack.Brushes.Text.Soft");

        var content = new StackPanel();
        content.Children.Add(row);
        content.Children.Add(name);

        var card = new Border
        {
            Width = 170,
            Margin = new Thickness(0, 0, 10, 10),
            Padding = new Thickness(8, 10, 8, 8),
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(Color.FromArgb(0x1F, 0, 0, 0)),
            Cursor = Cursors.Hand,
            ToolTip = $"<jp:JetPackIcon Kind=\"{kind}\" />",
            Tag = kind,
            Child = content,
        };
        card.MouseLeftButtonUp += (_, _) => Copy(kind);
        return card;
    }

    private Border Swatch(JetPackIconKind kind, string background, string foreground)
    {
        var swatch = new Border { Width = 42, Height = 36, Margin = new Thickness(0, 0, 4, 0), Child = Icon(kind, foreground) };
        swatch.SetResourceReference(Border.BackgroundProperty, background);
        return swatch;
    }

    private JetPackIcon Icon(JetPackIconKind kind, string foreground)
    {
        var icon = new JetPackIcon { Kind = kind, Width = size, Height = size };
        icon.SetResourceReference(ForegroundProperty, foreground);
        icons.Add(icon);
        return icon;
    }

    private void Copy(JetPackIconKind kind)
    {
        var xaml = $"<jp:JetPackIcon Kind=\"{kind}\" />";
        try
        {
            Clipboard.SetText(xaml);
            Copied.Text = "Copied " + xaml;
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            // Another program holds the clipboard
            Copied.Text = xaml;
        }
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        var text = Search.Text.Trim();
        foreach (Border card in Icons.Children)
        {
            card.Visibility = text.Length == 0 || card.Tag.ToString()!.Contains(text, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private void OnSizeChecked(object sender, RoutedEventArgs e)
    {
        // Checked fires during InitializeComponent, before the cards exist
        if (sender is RadioButton { Tag: string tag })
        {
            size = double.Parse(tag);
            foreach (var icon in icons)
            {
                icon.Width = icon.Height = size;
            }
        }
    }
}
