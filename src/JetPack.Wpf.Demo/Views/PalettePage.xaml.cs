using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace JetPack.Wpf.Demo.Views;

public partial class PalettePage : UserControl
{
    private readonly List<(StackPanel Group, List<Border> Cards)> _groups = [];
    private int _total;

    public PalettePage()
    {
        InitializeComponent();
        foreach (var (title, keys) in PaletteGroups.Sort(Keys(Application.Current.Resources)))
        {
            var cards = new WrapPanel { Margin = new Thickness(0, 0, 0, 24) };
            var group = new StackPanel();
            group.Children.Add(new TextBlock { Text = title, Margin = new Thickness(0, 0, 0, 10), Style = (Style)FindResource("JetPack.Styles.TextBlock.Heading5") });
            group.Children.Add(cards);
            Groups.Children.Add(group);

            var list = keys.Select(Card).ToList();
            list.ForEach(c => cards.Children.Add(c));
            _groups.Add((group, list));
            _total += list.Count;
        }
        UpdateCount(_total);
    }

    // Every key of the dictionary and the dictionaries it merges (the accent ThemeManager adds last included)
    private static IEnumerable<string> Keys(ResourceDictionary dictionary) =>
        dictionary.Keys.OfType<string>().Concat(dictionary.MergedDictionaries.SelectMany(Keys));

    private static Border Card(string key)
    {
        // The resource lands in Tag, so it follows accent changes; the converter turns a Color into a brush
        var chip = new Border { Height = 44, BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0, 0, 0)), BorderThickness = new Thickness(0, 0, 0, 1) };
        chip.SetResourceReference(TagProperty, key);
        chip.SetBinding(Border.BackgroundProperty, new Binding(nameof(Tag)) { RelativeSource = RelativeSource.Self, Converter = ToBrush.Instance });

        var meta = new StackPanel { Margin = new Thickness(8, 6, 8, 8) };
        meta.Children.Add(new TextBlock { Text = PaletteGroups.Label(key), FontWeight = FontWeights.Bold, FontSize = 12 });
        // A zero-width space after each dot lets long keys wrap between their parts
        var code = new TextBlock { Text = key.Replace(".", ".​"), FontSize = 10.5, TextWrapping = TextWrapping.Wrap };
        code.SetResourceReference(TextBlock.FontFamilyProperty, "JetPack.Fonts.Family.Mono");
        code.SetResourceReference(TextBlock.ForegroundProperty, "JetPack.Brushes.Text.Soft");
        meta.Children.Add(code);

        var card = new StackPanel();
        card.Children.Add(chip);
        card.Children.Add(meta);
        return new Border
        {
            Width = 200,
            Margin = new Thickness(0, 0, 12, 12),
            CornerRadius = new CornerRadius(2),
            Background = new SolidColorBrush(Color.FromArgb(0x1F, 0, 0, 0)),
            Child = card,
            Tag = key,
        };
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        var text = Search.Text.Trim();
        var shown = 0;
        foreach (var (group, cards) in _groups)
        {
            var any = false;
            foreach (var card in cards)
            {
                var match = text.Length == 0 || card.Tag.ToString()!.Contains(text, StringComparison.OrdinalIgnoreCase);
                card.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
                any |= match;
                shown += match ? 1 : 0;
            }
            group.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
        }
        UpdateCount(shown);
    }

    private void UpdateCount(int shown) =>
        Count.Text = shown == _total ? $"{_total} brushes and colours" : $"{shown} of {_total}";

    private sealed class ToBrush : IValueConverter
    {
        public static readonly ToBrush Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is Color color ? new SolidColorBrush(color) : value as Brush;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
