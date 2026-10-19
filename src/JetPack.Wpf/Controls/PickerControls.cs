using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace JetPack.Wpf.Controls;

/// <summary>Time input with a drop-down list of times in <see cref="MinuteStep"/> steps.</summary>
[TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
[TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
[TemplatePart(Name = "PART_List", Type = typeof(ListBox))]
public class TimePicker : Control
{
    private TextBox? textBox;
    private ListBox? list;
    private bool updating;

    static TimePicker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TimePicker), new FrameworkPropertyMetadata(typeof(TimePicker)));
    }

    public static readonly DependencyProperty SelectedTimeProperty = DependencyProperty.Register(
        nameof(SelectedTime), typeof(TimeSpan?), typeof(TimePicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((TimePicker)d).UpdateText()));

    public TimeSpan? SelectedTime
    {
        get => (TimeSpan?)GetValue(SelectedTimeProperty);
        set => SetValue(SelectedTimeProperty, value);
    }

    public static readonly DependencyProperty MinuteStepProperty = DependencyProperty.Register(
        nameof(MinuteStep), typeof(int), typeof(TimePicker), new PropertyMetadata(15, (d, _) => ((TimePicker)d).FillList()));

    public int MinuteStep
    {
        get => (int)GetValue(MinuteStepProperty);
        set => SetValue(MinuteStepProperty, value);
    }

    public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
        nameof(IsDropDownOpen), typeof(bool), typeof(TimePicker), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDropDownChanged));

    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        textBox = GetTemplateChild("PART_TextBox") as TextBox;
        list = GetTemplateChild("PART_List") as ListBox;
        if (textBox != null)
        {
            textBox.LostKeyboardFocus += (_, _) => Commit();
            textBox.KeyDown += (_, e) => { if (e.Key == Key.Enter) Commit(); };
        }
        if (list != null)
        {
            list.SelectionChanged += (_, _) =>
            {
                if (!updating && list.SelectedItem is TimeSpan t)
                {
                    SelectedTime = t;
                    IsDropDownOpen = false;
                }
            };
        }
        FillList();
        UpdateText();
    }

    private static void OnDropDownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var p = (TimePicker)d;
        if ((bool)e.NewValue && p.list != null)
        {
            p.updating = true;
            p.list.SelectedItem = p.list.Items.Cast<TimeSpan>().LastOrDefault(t => p.SelectedTime is { } s && t <= s);
            p.updating = false;
            if (p.list.SelectedItem != null)
            {
                p.Dispatcher.BeginInvoke(() => p.list.ScrollIntoView(p.list.SelectedItem));
            }
        }
    }

    private void FillList()
    {
        if (list == null)
        {
            return;
        }
        var step = Math.Max(1, MinuteStep);
        list.Items.Clear();
        for (var m = 0; m < 24 * 60; m += step)
        {
            list.Items.Add(TimeSpan.FromMinutes(m));
        }
    }

    private void Commit()
    {
        if (textBox == null)
        {
            return;
        }
        var text = textBox.Text.Trim().Replace('.', ':');
        if (text.Length == 0)
        {
            SelectedTime = null;
        }
        else if (TimeSpan.TryParse(text, CultureInfo.CurrentCulture, out var t) && t < TimeSpan.FromDays(1))
        {
            SelectedTime = t;
        }
        else if (int.TryParse(text, out var hours) && hours is >= 0 and < 24)
        {
            SelectedTime = TimeSpan.FromHours(hours);
        }
        UpdateText();
    }

    private void UpdateText()
    {
        if (textBox != null)
        {
            textBox.Text = SelectedTime?.ToString(@"hh\:mm") ?? string.Empty;
        }
    }
}

/// <summary>Colour drop-down with a swatch grid.</summary>
[TemplatePart(Name = "PART_Palette", Type = typeof(ListBox))]
public class ColorPicker : Control
{
    private ListBox? palette;

    public static readonly IReadOnlyList<Color> DefaultPalette = new[]
    {
        "#75A20A", "#91B006", "#C2DF00", "#FE770A", "#FFB400", "#148CD1", "#42BAFF", "#4599CA",
        "#DC000C", "#EF0A0A", "#8E44AD", "#2E7D32", "#202020", "#57575C", "#707176", "#C6C6C6",
        "#E0E0E0", "#FFFFFF", "#B62F00", "#00538E", "#2B3D00", "#686868", "#DCDCDD", "#F7F7F7",
    }.Select(s => (Color)ColorConverter.ConvertFromString(s)).ToArray();

    static ColorPicker()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPicker), new FrameworkPropertyMetadata(typeof(ColorPicker)));
    }

    public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
        nameof(SelectedColor), typeof(Color), typeof(ColorPicker),
        new FrameworkPropertyMetadata(Color.FromRgb(0x75, 0xA2, 0x0A), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((ColorPicker)d).Sync()));

    public Color SelectedColor
    {
        get => (Color)GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    public static readonly DependencyProperty PaletteProperty = DependencyProperty.Register(
        nameof(Palette), typeof(IEnumerable), typeof(ColorPicker), new PropertyMetadata(DefaultPalette));

    public IEnumerable Palette
    {
        get => (IEnumerable)GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
        nameof(IsDropDownOpen), typeof(bool), typeof(ColorPicker), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        palette = GetTemplateChild("PART_Palette") as ListBox;
        if (palette != null)
        {
            palette.SelectionChanged += (_, _) =>
            {
                if (palette.SelectedItem is Color c && c != SelectedColor)
                {
                    SelectedColor = c;
                    IsDropDownOpen = false;
                }
            };
        }
        Sync();
    }

    private void Sync()
    {
        if (palette != null)
        {
            palette.SelectedItem = Palette.Cast<object>().OfType<Color>().Contains(SelectedColor) ? SelectedColor : null;
        }
    }
}

/// <summary>Text box that suggests matching entries from <see cref="ItemsSource"/> while typing.</summary>
[TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
[TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
[TemplatePart(Name = "PART_List", Type = typeof(ListBox))]
public class AutoSuggestBox : Control
{
    private TextBox? textBox;
    private Popup? popup;
    private ListBox? list;
    private bool choosing;

    static AutoSuggestBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AutoSuggestBox), new FrameworkPropertyMetadata(typeof(AutoSuggestBox)));
    }

    public static readonly RoutedEvent SuggestionChosenEvent = EventManager.RegisterRoutedEvent(
        nameof(SuggestionChosen), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AutoSuggestBox));

    public event RoutedEventHandler SuggestionChosen
    {
        add => AddHandler(SuggestionChosenEvent, value);
        remove => RemoveHandler(SuggestionChosenEvent, value);
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(AutoSuggestBox), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(AutoSuggestBox), new PropertyMetadata(null));

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty MaxSuggestionsProperty = DependencyProperty.Register(
        nameof(MaxSuggestions), typeof(int), typeof(AutoSuggestBox), new PropertyMetadata(8));

    public int MaxSuggestions
    {
        get => (int)GetValue(MaxSuggestionsProperty);
        set => SetValue(MaxSuggestionsProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        textBox = GetTemplateChild("PART_TextBox") as TextBox;
        popup = GetTemplateChild("PART_Popup") as Popup;
        list = GetTemplateChild("PART_List") as ListBox;

        if (textBox != null)
        {
            textBox.TextChanged += (_, _) => { if (!choosing) Suggest(); };
            textBox.PreviewKeyDown += OnTextKeyDown;
            textBox.LostKeyboardFocus += (_, e) =>
            {
                if (popup != null && list != null && !list.IsKeyboardFocusWithin)
                {
                    popup.IsOpen = false;
                }
            };
        }
        if (list != null)
        {
            list.PreviewMouseLeftButtonUp += (_, _) => Choose();
            list.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter) { Choose(); e.Handled = true; }
                if (e.Key == Key.Escape && popup != null) { popup.IsOpen = false; textBox?.Focus(); }
            };
        }
    }

    private void OnTextKeyDown(object sender, KeyEventArgs e)
    {
        if (popup is not { IsOpen: true } || list == null)
        {
            return;
        }
        switch (e.Key)
        {
            case Key.Down:
                list.SelectedIndex = Math.Min(list.Items.Count - 1, list.SelectedIndex + 1);
                e.Handled = true;
                break;
            case Key.Up:
                list.SelectedIndex = Math.Max(0, list.SelectedIndex - 1);
                e.Handled = true;
                break;
            case Key.Enter:
                Choose();
                e.Handled = true;
                break;
            case Key.Escape:
                popup.IsOpen = false;
                e.Handled = true;
                break;
        }
    }

    private void Suggest()
    {
        if (popup == null || list == null || ItemsSource == null)
        {
            return;
        }
        var text = textBox?.Text ?? string.Empty;
        var matches = text.Length == 0
            ? new List<object>()
            : ItemsSource.Cast<object>()
                .Where(i => i?.ToString()?.Contains(text, StringComparison.CurrentCultureIgnoreCase) == true)
                .Take(MaxSuggestions)
                .ToList();
        list.ItemsSource = matches;
        list.SelectedIndex = matches.Count > 0 ? 0 : -1;
        popup.IsOpen = matches.Count > 0 && IsKeyboardFocusWithin;
    }

    private void Choose()
    {
        if (list?.SelectedItem is not { } item || textBox == null)
        {
            return;
        }
        choosing = true;
        Text = item.ToString() ?? string.Empty;
        textBox.CaretIndex = textBox.Text.Length;
        choosing = false;
        if (popup != null)
        {
            popup.IsOpen = false;
        }
        textBox.Focus();
        RaiseEvent(new RoutedEventArgs(SuggestionChosenEvent, item));
    }
}
