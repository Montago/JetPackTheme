using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace JetPack.Wpf.Controls;

/// <summary>How <see cref="SearchComboBox"/> matches typed text against the items.</summary>
public enum SearchFilterMode
{
    Contains,
    StartsWith,
}

/// <summary>
/// Combo box that can be opened and clicked like a <see cref="ComboBox"/>, or typed into to filter
/// and complete the items. The drop-down is virtualized, so lists of thousands of items stay fast.
/// </summary>
[TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox))]
[TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
[TemplatePart(Name = "PART_List", Type = typeof(ListBox))]
public class SearchComboBox : Control
{
    private readonly record struct Entry(object Item, string Text);

    private TextBox? textBox;
    private ListBox? list;
    private Window? window;
    private INotifyCollectionChanged? observed;
    private List<Entry> entries = new();
    private List<object> allItems = new();

    // Typed text the list is filtered by; null while the box shows the selected item.
    private string? filterText;
    // Item that leaving the box or Tab commits; follows the highlight once the user types or navigates.
    private object? pending;
    private bool updating;
    private bool syncing;

    static SearchComboBox()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchComboBox), new FrameworkPropertyMetadata(typeof(SearchComboBox)));
    }

    public SearchComboBox()
    {
        Unloaded += (_, _) => IsDropDownOpen = false;
    }

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(SearchComboBox), new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
        nameof(SelectedItem), typeof(object), typeof(SearchComboBox),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public static readonly DependencyProperty SelectedValueProperty = DependencyProperty.Register(
        nameof(SelectedValue), typeof(object), typeof(SearchComboBox),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedValueChanged));

    /// <summary>The <see cref="SelectedValuePath"/> member of <see cref="SelectedItem"/> (the item itself without a path).</summary>
    public object? SelectedValue
    {
        get => GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }

    public static readonly DependencyProperty SelectedValuePathProperty = DependencyProperty.Register(
        nameof(SelectedValuePath), typeof(string), typeof(SearchComboBox), new PropertyMetadata(string.Empty, OnSelectedValuePathChanged));

    public string SelectedValuePath
    {
        get => (string)GetValue(SelectedValuePathProperty);
        set => SetValue(SelectedValuePathProperty, value);
    }

    public static readonly DependencyProperty DisplayMemberPathProperty = DependencyProperty.Register(
        nameof(DisplayMemberPath), typeof(string), typeof(SearchComboBox), new PropertyMetadata(string.Empty, OnDisplayMemberPathChanged));

    /// <summary>Member that gives the text shown, matched and completed; <c>ToString()</c> when empty.</summary>
    public string DisplayMemberPath
    {
        get => (string)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
        nameof(ItemTemplate), typeof(DataTemplate), typeof(SearchComboBox), new PropertyMetadata(null));

    /// <summary>Template for the items in the drop-down; the text box still shows <see cref="DisplayMemberPath"/>.</summary>
    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    private static readonly DependencyPropertyKey TextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Text), typeof(string), typeof(SearchComboBox), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TextProperty = TextPropertyKey.DependencyProperty;

    /// <summary>The text in the box, including any completion that is still selected.</summary>
    public string Text => (string)GetValue(TextProperty);

    public static readonly DependencyProperty FilterModeProperty = DependencyProperty.Register(
        nameof(FilterMode), typeof(SearchFilterMode), typeof(SearchComboBox), new PropertyMetadata(SearchFilterMode.Contains));

    public SearchFilterMode FilterMode
    {
        get => (SearchFilterMode)GetValue(FilterModeProperty);
        set => SetValue(FilterModeProperty, value);
    }

    public static readonly DependencyProperty IsFilteringEnabledProperty = DependencyProperty.Register(
        nameof(IsFilteringEnabled), typeof(bool), typeof(SearchComboBox), new PropertyMetadata(true));

    /// <summary>When false, typing only highlights and completes; the drop-down keeps every item.</summary>
    public bool IsFilteringEnabled
    {
        get => (bool)GetValue(IsFilteringEnabledProperty);
        set => SetValue(IsFilteringEnabledProperty, value);
    }

    public static readonly DependencyProperty IsAutoCompleteEnabledProperty = DependencyProperty.Register(
        nameof(IsAutoCompleteEnabled), typeof(bool), typeof(SearchComboBox), new PropertyMetadata(true));

    /// <summary>Fills in the rest of the first item that starts with the typed text, selected so the next key replaces it.</summary>
    public bool IsAutoCompleteEnabled
    {
        get => (bool)GetValue(IsAutoCompleteEnabledProperty);
        set => SetValue(IsAutoCompleteEnabledProperty, value);
    }

    public static readonly DependencyProperty OpenOnFocusProperty = DependencyProperty.Register(
        nameof(OpenOnFocus), typeof(bool), typeof(SearchComboBox), new PropertyMetadata(true));

    public bool OpenOnFocus
    {
        get => (bool)GetValue(OpenOnFocusProperty);
        set => SetValue(OpenOnFocusProperty, value);
    }

    public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
        nameof(IsDropDownOpen), typeof(bool), typeof(SearchComboBox),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDropDownChanged));

    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    public static readonly DependencyProperty MaxDropDownHeightProperty = DependencyProperty.Register(
        nameof(MaxDropDownHeight), typeof(double), typeof(SearchComboBox), new PropertyMetadata(260.0));

    public double MaxDropDownHeight
    {
        get => (double)GetValue(MaxDropDownHeightProperty);
        set => SetValue(MaxDropDownHeightProperty, value);
    }

    public static readonly DependencyProperty NoMatchesTextProperty = DependencyProperty.Register(
        nameof(NoMatchesText), typeof(string), typeof(SearchComboBox), new PropertyMetadata("No matches"));

    /// <summary>Shown in the drop-down when no item matches the typed text.</summary>
    public string NoMatchesText
    {
        get => (string)GetValue(NoMatchesTextProperty);
        set => SetValue(NoMatchesTextProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        if (textBox != null)
        {
            textBox.TextChanged -= OnTextChanged;
            textBox.PreviewKeyDown -= OnTextKeyDown;
            textBox.GotKeyboardFocus -= OnTextGotFocus;
        }
        if (list != null)
        {
            list.PreviewMouseLeftButtonDown -= OnListMouseDown;
        }

        textBox = GetTemplateChild("PART_TextBox") as TextBox;
        list = GetTemplateChild("PART_List") as ListBox;

        if (textBox != null)
        {
            textBox.TextChanged += OnTextChanged;
            textBox.PreviewKeyDown += OnTextKeyDown;
            textBox.GotKeyboardFocus += OnTextGotFocus;
        }
        if (list != null)
        {
            list.PreviewMouseLeftButtonDown += OnListMouseDown;
        }
        ShowSelected();
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);
        // Focus() on the control itself (or a Label target) goes on to the text box.
        if (e.NewFocus == this && textBox != null)
        {
            textBox.Focus();
            e.Handled = true;
        }
    }

    protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnIsKeyboardFocusWithinChanged(e);
        if (!(bool)e.NewValue)
        {
            IsDropDownOpen = false;
            CommitText();
        }
    }

    private void OnTextGotFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // Tabbing in selects the text so typing replaces it; a click places the caret as usual.
        if (Mouse.LeftButton == MouseButtonState.Released)
        {
            textBox!.SelectAll();
        }
        // No old focus means the window is being activated again, not that the user came to this box.
        if (OpenOnFocus && e.OldFocus != null && !IsDropDownOpen)
        {
            IsDropDownOpen = true;
        }
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        SetValue(TextPropertyKey, textBox!.Text);
        if (updating)
        {
            return;
        }

        var text = textBox.Text;
        filterText = text;
        pending = text.Length == 0 ? null : FindBest(text);

        // Complete only when something was typed or pasted, not when Backspace or Delete removed text.
        if (IsAutoCompleteEnabled && pending != null && e.Changes.Any(c => c.AddedLength > 0) && textBox.CaretIndex == text.Length)
        {
            var best = GetText(pending);
            if (best.Length > text.Length && best.StartsWith(text, StringComparison.CurrentCultureIgnoreCase))
            {
                // Keeps what the user typed; the item's own casing appears once it is committed.
                SetBoxText(text + best[text.Length..]);
                textBox.Select(text.Length, best.Length - text.Length);
            }
        }

        if (IsDropDownOpen)
        {
            Refresh();
        }
        else if (IsKeyboardFocusWithin)
        {
            IsDropDownOpen = true;
        }
    }

    private void OnTextKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        switch (key)
        {
            case Key.Down when Keyboard.Modifiers == ModifierKeys.Alt:
            case Key.Up when Keyboard.Modifiers == ModifierKeys.Alt:
            case Key.F4:
                IsDropDownOpen = !IsDropDownOpen;
                e.Handled = true;
                break;
            case Key.Down:
                Move(1, e);
                break;
            case Key.Up:
                Move(-1, e);
                break;
            case Key.PageDown:
                Move(PageSize(), e);
                break;
            case Key.PageUp:
                Move(-PageSize(), e);
                break;
            case Key.Enter:
                if (IsDropDownOpen)
                {
                    if (list?.SelectedItem is { } chosen)
                    {
                        Commit(chosen, closeAfter: true);
                    }
                    else
                    {
                        IsDropDownOpen = false;
                        CommitText();
                    }
                    textBox?.SelectAll();
                    e.Handled = true;
                }
                else
                {
                    CommitText();
                }
                break;
            case Key.Tab:
                // Not handled, so focus still moves on to the next control.
                if (IsDropDownOpen && list?.SelectedItem is { } highlighted)
                {
                    Commit(highlighted, closeAfter: true);
                }
                break;
            case Key.Escape:
                if (IsDropDownOpen || filterText != null || pending != null)
                {
                    IsDropDownOpen = false;
                    ShowSelected();
                    textBox?.SelectAll();
                    e.Handled = true;
                }
                break;
        }
    }

    private void Move(int delta, KeyEventArgs e)
    {
        e.Handled = true;
        if (!IsDropDownOpen)
        {
            IsDropDownOpen = true;
            return;
        }
        if (list == null || list.Items.Count == 0)
        {
            return;
        }
        var index = list.SelectedIndex < 0 && delta > 0 ? 0 : Math.Clamp(list.SelectedIndex + delta, 0, list.Items.Count - 1);
        list.SelectedIndex = index;
        list.ScrollIntoView(list.SelectedItem);
        pending = list.SelectedItem;
        SetBoxText(GetText(pending));
        textBox?.SelectAll();
    }

    private int PageSize()
    {
        var item = list?.ItemContainerGenerator.ContainerFromIndex(Math.Max(0, list.SelectedIndex)) as FrameworkElement;
        return item is { ActualHeight: > 0 } ? Math.Max(1, (int)(MaxDropDownHeight / item.ActualHeight) - 1) : 10;
    }

    private void OnListMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Picks the item on press and keeps keyboard focus in the text box; scroll bar clicks pass through.
        if (ItemsControl.ContainerFromElement(list, (DependencyObject)e.OriginalSource) is ListBoxItem container)
        {
            Commit(list!.ItemContainerGenerator.ItemFromContainer(container), closeAfter: true);
            textBox?.Focus();
            if (textBox != null)
            {
                textBox.CaretIndex = textBox.Text.Length;
            }
            e.Handled = true;
        }
    }

    private static void OnDropDownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (SearchComboBox)d;
        if ((bool)e.NewValue)
        {
            c.Refresh();
            c.HookWindow();
            if (!c.IsKeyboardFocusWithin && c.IsLoaded)
            {
                c.textBox?.Focus();
            }
            // The list only has containers once the popup has been laid out.
            c.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                if (c.list?.SelectedItem is { } item)
                {
                    c.list.ScrollIntoView(item);
                }
            });
        }
        else
        {
            c.UnhookWindow();
        }
    }

    // A popup that stays open does not see clicks elsewhere, so close it from the window instead.
    private void HookWindow()
    {
        UnhookWindow();
        window = Window.GetWindow(this);
        if (window != null)
        {
            window.PreviewMouseDown += OnWindowMouseDown;
            window.Deactivated += OnWindowMoved;
            window.LocationChanged += OnWindowMoved;
            window.SizeChanged += OnWindowMoved;
        }
    }

    private void UnhookWindow()
    {
        if (window != null)
        {
            window.PreviewMouseDown -= OnWindowMouseDown;
            window.Deactivated -= OnWindowMoved;
            window.LocationChanged -= OnWindowMoved;
            window.SizeChanged -= OnWindowMoved;
            window = null;
        }
    }

    private void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsMouseOver)
        {
            IsDropDownOpen = false;
        }
    }

    private void OnWindowMoved(object? sender, EventArgs e) => IsDropDownOpen = false;

    /// <summary>Fills the drop-down from the current filter and highlights the item that would be committed.</summary>
    private void Refresh()
    {
        if (list == null)
        {
            return;
        }
        var showAll = string.IsNullOrEmpty(filterText) || !IsFilteringEnabled;
        var items = showAll ? allItems : Filter(filterText!).Select(x => x.Item).ToList();
        if (!ReferenceEquals(list.ItemsSource, items))
        {
            list.ItemsSource = items;
        }
        var highlight = pending ?? (filterText == null ? SelectedItem : null);
        list.SelectedItem = highlight;
        if (highlight != null && list.IsLoaded)
        {
            list.ScrollIntoView(highlight);
        }
        else if (items.Count > 0 && list.IsLoaded)
        {
            list.ScrollIntoView(items[0]);
        }
    }

    private IEnumerable<Entry> Filter(string text) => FilterMode == SearchFilterMode.StartsWith
        ? entries.Where(x => x.Text.StartsWith(text, StringComparison.CurrentCultureIgnoreCase))
        : entries.Where(x => x.Text.Contains(text, StringComparison.CurrentCultureIgnoreCase));

    // The first item starting with the text, else (when filtering by Contains) the first one containing it.
    private object? FindBest(string text)
    {
        foreach (var x in entries)
        {
            if (x.Text.StartsWith(text, StringComparison.CurrentCultureIgnoreCase))
            {
                return x.Item;
            }
        }
        return FilterMode == SearchFilterMode.Contains ? Filter(text).Select(x => x.Item).FirstOrDefault() : null;
    }

    /// <summary>Decides what the typed text means when the user leaves the box or presses Enter.</summary>
    private void CommitText()
    {
        if (textBox == null || (filterText == null && pending == null))
        {
            return;
        }
        var text = textBox.Text;
        if (text.Length == 0)
        {
            Commit(null, closeAfter: false);
        }
        else if (pending != null)
        {
            Commit(pending, closeAfter: false);
        }
        else
        {
            var exact = entries.Where(x => string.Equals(x.Text, text, StringComparison.CurrentCultureIgnoreCase)).Select(x => x.Item).FirstOrDefault();
            // Text that matches nothing snaps back to the selected item.
            Commit(exact ?? SelectedItem, closeAfter: false);
        }
    }

    private void Commit(object? item, bool closeAfter)
    {
        if (closeAfter)
        {
            IsDropDownOpen = false;
        }
        SelectedItem = item;
        ShowSelected();
    }

    private void ShowSelected()
    {
        filterText = null;
        pending = null;
        SetBoxText(GetText(SelectedItem));
    }

    private void SetBoxText(string text)
    {
        if (textBox == null || textBox.Text == text)
        {
            return;
        }
        updating = true;
        textBox.Text = text;
        textBox.CaretIndex = text.Length;
        updating = false;
    }

    private string GetText(object? item) =>
        item == null ? string.Empty : Convert.ToString(GetMember(item, DisplayMemberPath), CultureInfo.CurrentCulture) ?? string.Empty;

    private static object? GetMember(object? item, string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return item;
        }
        foreach (var name in path.Split('.'))
        {
            if (item == null)
            {
                return null;
            }
            item = item.GetType().GetProperty(name)?.GetValue(item);
        }
        return item;
    }

    private void RebuildEntries()
    {
        allItems = ItemsSource?.Cast<object>().Where(i => i != null).ToList() ?? new List<object>();
        entries = allItems.Select(i => new Entry(i, GetText(i))).ToList();
        if (SelectedItem == null && SelectedValue != null)
        {
            SelectFromValue();
        }
        if (IsDropDownOpen)
        {
            Refresh();
        }
        else if (list != null)
        {
            list.ItemsSource = null;
        }
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (SearchComboBox)d;
        if (c.observed != null)
        {
            CollectionChangedEventManager.RemoveHandler(c.observed, c.OnCollectionChanged);
        }
        c.observed = e.NewValue as INotifyCollectionChanged;
        if (c.observed != null)
        {
            CollectionChangedEventManager.AddHandler(c.observed, c.OnCollectionChanged);
        }
        c.RebuildEntries();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildEntries();

    private static void OnDisplayMemberPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (SearchComboBox)d;
        c.RebuildEntries();
        c.ShowSelected();
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (SearchComboBox)d;
        if (!c.syncing)
        {
            c.syncing = true;
            c.SelectedValue = GetMember(e.NewValue, c.SelectedValuePath);
            c.syncing = false;
        }
        c.ShowSelected();
    }

    private static void OnSelectedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (SearchComboBox)d;
        if (!c.syncing)
        {
            c.SelectFromValue();
        }
    }

    private static void OnSelectedValuePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (SearchComboBox)d;
        if (c.SelectedItem != null)
        {
            c.syncing = true;
            c.SelectedValue = GetMember(c.SelectedItem, c.SelectedValuePath);
            c.syncing = false;
        }
    }

    // Leaves SelectedValue alone when no item has it yet; it is looked up again when the items arrive.
    private void SelectFromValue()
    {
        var value = SelectedValue;
        var item = value == null ? null : allItems.FirstOrDefault(i => Equals(GetMember(i, SelectedValuePath), value));
        syncing = true;
        SelectedItem = item;
        syncing = false;
    }
}
