using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace JetPack.Wpf.Controls;

/// <summary>Event data for <see cref="JetPackGrid.Filtered"/>.</summary>
public sealed class GridFilteredEventArgs : RoutedEventArgs
{
    public GridFilteredEventArgs(RoutedEvent routedEvent, object source, int count, int totalCount) : base(routedEvent, source)
    {
        Count = count;
        TotalCount = totalCount;
    }

    /// <summary>Rows left after the search and the column filters.</summary>
    public int Count { get; }

    /// <summary>Rows in the source.</summary>
    public int TotalCount { get; }

    protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget) =>
        ((GridFilteredEventHandler)genericHandler)(genericTarget, this);
}

public delegate void GridFilteredEventHandler(object sender, GridFilteredEventArgs e);

/// <summary>One value in the tick list of a <see cref="JetPackGrid"/> header filter.</summary>
public sealed class GridFilterOption : INotifyPropertyChanged
{
    private readonly Action<GridFilterOption> changed;
    private bool isChecked;

    internal GridFilterOption(string text, int count, Action<GridFilterOption> changed)
    {
        Text = text;
        Count = count;
        this.changed = changed;
    }

    /// <summary>The value as the cells show it.</summary>
    public string Text { get; }

    /// <summary>Rows in the source with this value.</summary>
    public int Count { get; }

    /// <summary>Whether the value is in the column's FilterText; ticking it adds the value there.</summary>
    public bool IsChecked
    {
        get => isChecked;
        set
        {
            if (SetChecked(value))
            {
                changed(this);
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    // Follows FilterText without writing back to it.
    internal bool SetChecked(bool value)
    {
        if (isChecked == value)
        {
            return false;
        }
        isChecked = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
        return true;
    }
}

/// <summary>
/// Data grid for large collections: a word search across the columns with the hits marked in the
/// cells, a value filter in every column header, three-state sorting and grouping. Rows and columns
/// are virtualized. Columns are defined by hand (<see cref="JetPackGridColumn"/> or any DataGrid column).
/// </summary>
public class JetPackGrid : DataGrid
{
    /// <summary>Clears the filter of the column (or header) in the parameter; without one, clears the search and every filter.</summary>
    public static readonly RoutedUICommand ClearFilterCommand = new("Clear", nameof(ClearFilterCommand), typeof(JetPackGrid));

    /// <summary>Closes the filter box of the column header in the parameter.</summary>
    public static readonly RoutedUICommand CloseFilterCommand = new("Done", nameof(CloseFilterCommand), typeof(JetPackGrid));

    /// <summary>Key of the <see cref="System.Windows.Controls.GroupStyle"/> used when <see cref="GroupMemberPath"/> is set and no GroupStyle is given.</summary>
    public const string GroupStyleKey = "JetPack.GroupStyles.JetPackGrid";

    private readonly DispatcherTimer timer;
    private object? lastSelected;

    static JetPackGrid()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(JetPackGrid), new FrameworkPropertyMetadata(typeof(JetPackGrid)));
        AutoGenerateColumnsProperty.OverrideMetadata(typeof(JetPackGrid), new FrameworkPropertyMetadata(false));
        CommandManager.RegisterClassCommandBinding(typeof(JetPackGrid), new CommandBinding(ClearFilterCommand, OnClearFilter));
        CommandManager.RegisterClassCommandBinding(typeof(JetPackGrid), new CommandBinding(CloseFilterCommand, OnCloseFilter));
        // The filter box lives in a popup of the column header; its events still bubble up to the grid.
        EventManager.RegisterClassHandler(typeof(JetPackGrid), ToggleButton.CheckedEvent, new RoutedEventHandler(OnFilterButtonChecked));
        EventManager.RegisterClassHandler(typeof(JetPackGrid), PreviewKeyDownEvent, new KeyEventHandler(OnFilterBoxKeyDown));
    }

    public JetPackGrid()
    {
        timer = new DispatcherTimer(DispatcherPriority.Input);
        timer.Tick += (_, _) => ApplyFilters();
        Columns.CollectionChanged += OnColumnsChanged;
        Loaded += (_, _) => EnsureGroupStyle();
    }

    // ---------------- Search ----------------

    public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register(
        nameof(SearchText), typeof(string), typeof(JetPackGrid),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((JetPackGrid)d).ScheduleFilter()));

    /// <summary>Words that must all be found in a row (in any searchable column); quotes keep a phrase together.</summary>
    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public static readonly DependencyProperty SearchDelayProperty = DependencyProperty.Register(
        nameof(SearchDelay), typeof(TimeSpan), typeof(JetPackGrid), new PropertyMetadata(TimeSpan.FromMilliseconds(150)));

    /// <summary>Pause after the last change to the search or a filter before the rows are filtered; zero filters at once.</summary>
    public TimeSpan SearchDelay
    {
        get => (TimeSpan)GetValue(SearchDelayProperty);
        set => SetValue(SearchDelayProperty, value);
    }

    public static readonly DependencyProperty FilterListLimitProperty = DependencyProperty.Register(
        nameof(FilterListLimit), typeof(int), typeof(JetPackGrid), new PropertyMetadata(20));

    /// <summary>
    /// A header filter lists the column's values to tick when the column has at most this many
    /// different values; with more, they are typed. Zero never shows the list.
    /// </summary>
    public int FilterListLimit
    {
        get => (int)GetValue(FilterListLimitProperty);
        set => SetValue(FilterListLimitProperty, value);
    }

    private static readonly DependencyPropertyKey SearchWordsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(SearchWords), typeof(IReadOnlyList<string>), typeof(JetPackGrid), new PropertyMetadata(Array.Empty<string>()));

    public static readonly DependencyProperty SearchWordsProperty = SearchWordsPropertyKey.DependencyProperty;

    /// <summary>The words of <see cref="SearchText"/> the rows are filtered by and the cells mark.</summary>
    public IReadOnlyList<string> SearchWords => (IReadOnlyList<string>)GetValue(SearchWordsProperty);

    public static readonly DependencyProperty HighlightBrushProperty = DependencyProperty.Register(
        nameof(HighlightBrush), typeof(Brush), typeof(JetPackGrid), new PropertyMetadata(null));

    /// <summary>Background of the search hits in <see cref="JetPackGridColumn"/> cells.</summary>
    public Brush? HighlightBrush
    {
        get => (Brush?)GetValue(HighlightBrushProperty);
        set => SetValue(HighlightBrushProperty, value);
    }

    public static readonly DependencyProperty HighlightForegroundProperty = DependencyProperty.Register(
        nameof(HighlightForeground), typeof(Brush), typeof(JetPackGrid), new PropertyMetadata(null));

    public Brush? HighlightForeground
    {
        get => (Brush?)GetValue(HighlightForegroundProperty);
        set => SetValue(HighlightForegroundProperty, value);
    }

    // ---------------- Grouping and counts ----------------

    public static readonly DependencyProperty GroupMemberPathProperty = DependencyProperty.Register(
        nameof(GroupMemberPath), typeof(string), typeof(JetPackGrid), new PropertyMetadata(null, (d, _) => ((JetPackGrid)d).UpdateGrouping()));

    /// <summary>Member the rows are grouped by; empty for no groups.</summary>
    public string? GroupMemberPath
    {
        get => (string?)GetValue(GroupMemberPathProperty);
        set => SetValue(GroupMemberPathProperty, value);
    }

    private static readonly DependencyPropertyKey FilteredItemCountPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(FilteredItemCount), typeof(int), typeof(JetPackGrid), new PropertyMetadata(0));

    public static readonly DependencyProperty FilteredItemCountProperty = FilteredItemCountPropertyKey.DependencyProperty;

    /// <summary>Rows left after the search and the column filters.</summary>
    public int FilteredItemCount => (int)GetValue(FilteredItemCountProperty);

    private static readonly DependencyPropertyKey TotalItemCountPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(TotalItemCount), typeof(int), typeof(JetPackGrid), new PropertyMetadata(0));

    public static readonly DependencyProperty TotalItemCountProperty = TotalItemCountPropertyKey.DependencyProperty;

    /// <summary>Rows in the source, filtered or not.</summary>
    public int TotalItemCount => (int)GetValue(TotalItemCountProperty);

    private static readonly DependencyPropertyKey IsFilteredPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsFiltered), typeof(bool), typeof(JetPackGrid), new PropertyMetadata(false));

    public static readonly DependencyProperty IsFilteredProperty = IsFilteredPropertyKey.DependencyProperty;

    /// <summary>True while a search or a column filter is applied.</summary>
    public bool IsFiltered => (bool)GetValue(IsFilteredProperty);

    // ---------------- Column settings (attached to any DataGridColumn) ----------------

    public static readonly DependencyProperty IsSearchableProperty = DependencyProperty.RegisterAttached(
        "IsSearchable", typeof(bool), typeof(JetPackGrid), new PropertyMetadata(true, OnColumnSettingChanged));

    /// <summary>Whether the search looks in this column (and marks its hits).</summary>
    public static bool GetIsSearchable(DependencyObject column) => (bool)column.GetValue(IsSearchableProperty);
    public static void SetIsSearchable(DependencyObject column, bool value) => column.SetValue(IsSearchableProperty, value);

    public static readonly DependencyProperty IsFilterableProperty = DependencyProperty.RegisterAttached(
        "IsFilterable", typeof(bool), typeof(JetPackGrid), new PropertyMetadata(true, OnColumnSettingChanged));

    /// <summary>Whether the column header shows the filter button.</summary>
    public static bool GetIsFilterable(DependencyObject column) => (bool)column.GetValue(IsFilterableProperty);
    public static void SetIsFilterable(DependencyObject column, bool value) => column.SetValue(IsFilterableProperty, value);

    public static readonly DependencyProperty FilterMemberPathProperty = DependencyProperty.RegisterAttached(
        "FilterMemberPath", typeof(string), typeof(JetPackGrid), new PropertyMetadata(null, OnColumnSettingChanged));

    /// <summary>
    /// Member the search and the filter read for this column. Defaults to the column's Binding
    /// (with its StringFormat and Converter), or SortMemberPath for template columns.
    /// </summary>
    public static string? GetFilterMemberPath(DependencyObject column) => (string?)column.GetValue(FilterMemberPathProperty);
    public static void SetFilterMemberPath(DependencyObject column, string? value) => column.SetValue(FilterMemberPathProperty, value);

    public static readonly DependencyProperty FilterTextProperty = DependencyProperty.RegisterAttached(
        "FilterText", typeof(string), typeof(JetPackGrid),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFilterTextChanged));

    /// <summary>
    /// Values the column must equal (ignoring case), separated by spaces or commas; a row passes if it
    /// matches any of them. Quotes keep a value with spaces together; * matches any text ("vol*").
    /// </summary>
    public static string GetFilterText(DependencyObject column) => (string)column.GetValue(FilterTextProperty);
    public static void SetFilterText(DependencyObject column, string value) => column.SetValue(FilterTextProperty, value);

    private static readonly DependencyPropertyKey HasFilterPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "HasFilter", typeof(bool), typeof(JetPackGrid), new PropertyMetadata(false));

    public static readonly DependencyProperty HasFilterProperty = HasFilterPropertyKey.DependencyProperty;

    /// <summary>True when the column's <c>FilterText</c> holds at least one value.</summary>
    public static bool GetHasFilter(DependencyObject column) => (bool)column.GetValue(HasFilterProperty);

    private static readonly DependencyPropertyKey FilterOptionsPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "FilterOptions", typeof(IReadOnlyList<GridFilterOption>), typeof(JetPackGrid), new PropertyMetadata(null));

    public static readonly DependencyProperty FilterOptionsProperty = FilterOptionsPropertyKey.DependencyProperty;

    /// <summary>
    /// The column's different values, read when its filter box opens; null when the column has more
    /// than <see cref="FilterListLimit"/> of them.
    /// </summary>
    public static IReadOnlyList<GridFilterOption>? GetFilterOptions(DependencyObject column) =>
        (IReadOnlyList<GridFilterOption>?)column.GetValue(FilterOptionsProperty);

    private static readonly DependencyPropertyKey HeaderColumnPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "HeaderColumn", typeof(DataGridColumn), typeof(JetPackGrid), new PropertyMetadata(null));

    public static readonly DependencyProperty HeaderColumnProperty = HeaderColumnPropertyKey.DependencyProperty;

    /// <summary>
    /// The column of a <see cref="DataGridColumnHeader"/>, for bindings in header templates:
    /// <c>DataGridColumnHeader.Column</c> does not notify when the header gets its column.
    /// </summary>
    public static DataGridColumn? GetHeaderColumn(DependencyObject header) => (DataGridColumn?)header.GetValue(HeaderColumnProperty);

    public static readonly DependencyProperty TracksHeaderColumnProperty = DependencyProperty.RegisterAttached(
        "TracksHeaderColumn", typeof(int), typeof(JetPackGrid), new PropertyMetadata(-1, OnTracksHeaderColumnChanged));

    /// <summary>Bound to the header's DisplayIndex by the header style; it changes whenever the header gets a column.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static int GetTracksHeaderColumn(DependencyObject header) => (int)header.GetValue(TracksHeaderColumnProperty);

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void SetTracksHeaderColumn(DependencyObject header, int value) => header.SetValue(TracksHeaderColumnProperty, value);

    private static void OnTracksHeaderColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGridColumnHeader header)
        {
            header.SetValue(HeaderColumnPropertyKey, header.Column);
        }
    }

    // The grid a column belongs to, so a changed FilterText can reach it.
    private static readonly DependencyProperty OwnerProperty = DependencyProperty.RegisterAttached(
        "Owner", typeof(JetPackGrid), typeof(JetPackGrid), new PropertyMetadata(null));

    // ---------------- Events ----------------

    public static readonly RoutedEvent FilteredEvent = EventManager.RegisterRoutedEvent(
        nameof(Filtered), RoutingStrategy.Bubble, typeof(GridFilteredEventHandler), typeof(JetPackGrid));

    /// <summary>Raised after the rows have been filtered by the search and the column filters.</summary>
    public event GridFilteredEventHandler Filtered
    {
        add => AddHandler(FilteredEvent, value);
        remove => RemoveHandler(FilteredEvent, value);
    }

    public static readonly RoutedEvent SelectedItemChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(SelectedItemChanged), RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<object?>), typeof(JetPackGrid));

    /// <summary>Raised when <c>SelectedItem</c> changes (unlike SelectionChanged, not by combo boxes inside the cells).</summary>
    public event RoutedPropertyChangedEventHandler<object?> SelectedItemChanged
    {
        add => AddHandler(SelectedItemChangedEvent, value);
        remove => RemoveHandler(SelectedItemChangedEvent, value);
    }

    // ---------------- Public methods ----------------

    /// <summary>The rows left after the search and the filters, in the order shown (sorted, grouped).</summary>
    public IReadOnlyList<object> GetVisibleItems()
    {
        var items = new List<object>(Items.Count);
        foreach (var item in Items)
        {
            if (item != CollectionView.NewItemPlaceholder)
            {
                items.Add(item);
            }
        }
        return items;
    }

    /// <summary>Clears the search and every column filter.</summary>
    public void ClearFilters()
    {
        SetCurrentValue(SearchTextProperty, string.Empty);
        foreach (var column in Columns)
        {
            column.SetCurrentValue(FilterTextProperty, string.Empty);
        }
        ApplyFilters();
    }

    /// <summary>Filters the rows now instead of after <see cref="SearchDelay"/>; also re-reads changed item values.</summary>
    public void ApplyFilters()
    {
        timer.Stop();

        if (Items is IEditableCollectionView { IsEditingItem: true } or IEditableCollectionView { IsAddingNew: true })
        {
            // A refresh is not allowed during an edit; try again once the row is valid.
            if (!CommitEdit(DataGridEditingUnit.Row, true))
            {
                timer.Start();
                return;
            }
        }

        var culture = Language.GetSpecificCulture();
        var words = Tokenize(SearchText);
        if (!words.SequenceEqual(SearchWords, StringComparer.Ordinal))
        {
            SetValue(SearchWordsPropertyKey, words.ToArray());
        }

        var searched = words.Count == 0
            ? Array.Empty<ColumnReader>()
            : Columns.Where(GetIsSearchable).Select(c => ColumnReader.Create(c, culture)).OfType<ColumnReader>().ToArray();
        var filters = Columns
            .Where(c => GetIsFilterable(c) && GetHasFilter(c))
            .Select(c => ColumnReader.Create(c, culture) is { } reader ? new ColumnFilter(reader, Tokenize(GetFilterText(c)).Select(FilterValue.Parse).ToArray()) : null)
            .OfType<ColumnFilter>()
            .ToArray();
        var searchWords = words.ToArray();

        Predicate<object>? predicate = searchWords.Length == 0 && filters.Length == 0
            ? null
            : item => Matches(item, searchWords, searched, filters);

        if (Items.CanFilter && (predicate != null || Items.Filter != null))
        {
            // Setting a new predicate refreshes the view.
            Items.Filter = predicate;
        }

        SetValue(IsFilteredPropertyKey, predicate != null);
        UpdateCounts();
        RaiseEvent(new GridFilteredEventArgs(FilteredEvent, this, FilteredItemCount, TotalItemCount));
    }

    // ---------------- Overrides ----------------

    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
        base.OnItemsSourceChanged(oldValue, newValue);
        UpdateGrouping();
        ApplyFilters();
    }

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);
        UpdateCounts();
    }

    protected override void OnSelectionChanged(SelectionChangedEventArgs e)
    {
        base.OnSelectionChanged(e);
        var item = SelectedItem;
        if (!Equals(item, lastSelected))
        {
            var old = lastSelected;
            lastSelected = item;
            RaiseEvent(new RoutedPropertyChangedEventArgs<object?>(old, item, SelectedItemChangedEvent));
        }
    }

    /// <summary>Clicking a header sorts ascending, then descending, then back to the source order.</summary>
    protected override void OnSorting(DataGridSortingEventArgs e)
    {
        var column = e.Column;
        if (column.SortDirection != ListSortDirection.Descending)
        {
            base.OnSorting(e);
            return;
        }

        e.Handled = true;
        var path = column.SortMemberPath;
        using (Items.DeferRefresh())
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                // Shift keeps the other sorted columns.
                for (var i = Items.SortDescriptions.Count - 1; i >= 0; i--)
                {
                    if (Items.SortDescriptions[i].PropertyName == path)
                    {
                        Items.SortDescriptions.RemoveAt(i);
                    }
                }
            }
            else
            {
                Items.SortDescriptions.Clear();
                foreach (var other in Columns)
                {
                    other.SortDirection = null;
                }
            }
        }
        column.SortDirection = null;
    }

    // ---------------- Implementation ----------------

    private void ScheduleFilter()
    {
        timer.Stop();
        if (SearchDelay <= TimeSpan.Zero)
        {
            ApplyFilters();
            return;
        }
        timer.Interval = SearchDelay;
        timer.Start();
    }

    private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (DataGridColumn column in e.OldItems)
            {
                if (column.GetValue(OwnerProperty) == this)
                {
                    column.ClearValue(OwnerProperty);
                }
            }
        }
        foreach (var column in Columns)
        {
            column.SetValue(OwnerProperty, this);
        }
        if (IsFiltered || Columns.Any(GetHasFilter))
        {
            ScheduleFilter();
        }
    }

    private static void OnColumnSettingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d.GetValue(OwnerProperty) is JetPackGrid grid && (grid.IsFiltered || GetHasFilter(d)))
        {
            grid.ScheduleFilter();
        }
    }

    private static void OnFilterTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var tokens = Tokenize((string?)e.NewValue);
        d.SetValue(HasFilterPropertyKey, tokens.Count > 0);
        foreach (var option in GetFilterOptions(d) ?? [])
        {
            option.SetChecked(tokens.Contains(option.Text, StringComparer.OrdinalIgnoreCase));
        }
        (d.GetValue(OwnerProperty) as JetPackGrid)?.ScheduleFilter();
    }

    /// <summary>Reads the column's different values for the tick list, unless there are more than <see cref="FilterListLimit"/>.</summary>
    private void LoadFilterOptions(DataGridColumn column)
    {
        List<GridFilterOption>? options = null;
        var culture = Language.GetSpecificCulture();
        if (FilterListLimit > 0 && ColumnReader.Create(column, culture) is { } reader)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in Items.SourceCollection ?? Items)
            {
                if (item == CollectionView.NewItemPlaceholder || reader.GetText(item).Trim() is not { Length: > 0 } text)
                {
                    continue;
                }
                if (counts.TryGetValue(text, out var count))
                {
                    counts[text] = count + 1;
                }
                else if (counts.Count == FilterListLimit)
                {
                    counts.Clear();
                    break;
                }
                else
                {
                    counts[text] = 1;
                }
            }

            if (counts.Count > 0)
            {
                // Numbers in numeric order, everything else alphabetically.
                var numbers = counts.Keys.ToDictionary(k => k, k => decimal.TryParse(k, NumberStyles.Any, culture, out var n) ? n : (decimal?)null);
                IEnumerable<string> keys = numbers.Values.All(n => n.HasValue)
                    ? counts.Keys.OrderBy(k => numbers[k])
                    : counts.Keys.OrderBy(k => k, StringComparer.Create(culture, true));
                options = keys.Select(k => new GridFilterOption(k, counts[k], _ => OnFilterOptionChanged(column))).ToList();
                var tokens = Tokenize(GetFilterText(column));
                foreach (var option in options)
                {
                    option.SetChecked(tokens.Contains(option.Text, StringComparer.OrdinalIgnoreCase));
                }
            }
        }
        column.SetValue(FilterOptionsPropertyKey, options);
    }

    /// <summary>Writes the ticked values into FilterText, keeping typed values that are not in the list (such as "vol*").</summary>
    private static void OnFilterOptionChanged(DataGridColumn column)
    {
        var options = GetFilterOptions(column) ?? [];
        var typed = Tokenize(GetFilterText(column)).Where(t => !options.Any(o => o.Text.Equals(t, StringComparison.OrdinalIgnoreCase)));
        var values = typed.Concat(options.Where(o => o.IsChecked).Select(o => o.Text))
            .Select(v => v.IndexOfAny([' ', ',', ';', '\t']) >= 0 ? $"\"{v}\"" : v);
        column.SetCurrentValue(FilterTextProperty, string.Join(", ", values));
    }

    private void UpdateGrouping()
    {
        var path = GroupMemberPath;
        var current = Items.GroupDescriptions.Count == 1 ? (Items.GroupDescriptions[0] as PropertyGroupDescription)?.PropertyName : null;
        if (current == path || (string.IsNullOrEmpty(path) && Items.GroupDescriptions.Count == 0))
        {
            return;
        }

        using (Items.DeferRefresh())
        {
            Items.GroupDescriptions.Clear();
            if (!string.IsNullOrEmpty(path))
            {
                Items.GroupDescriptions.Add(new PropertyGroupDescription(path));
            }
        }
        EnsureGroupStyle();
    }

    private void EnsureGroupStyle()
    {
        if (GroupStyle.Count == 0 && !string.IsNullOrEmpty(GroupMemberPath) && TryFindResource(GroupStyleKey) is GroupStyle style)
        {
            GroupStyle.Add(style);
        }
    }

    private void UpdateCounts()
    {
        var count = Items.Count;
        if (count > 0 && Items is IEditableCollectionView { NewItemPlaceholderPosition: not NewItemPlaceholderPosition.None })
        {
            count--;
        }
        var total = Items.SourceCollection switch
        {
            ICollection collection => collection.Count,
            IEnumerable source when Items.Filter != null => source.Cast<object>().Count(),
            _ => count,
        };
        SetValue(FilteredItemCountPropertyKey, count);
        SetValue(TotalItemCountPropertyKey, total);
    }

    private static bool Matches(object item, string[] words, ColumnReader[] searched, ColumnFilter[] filters)
    {
        foreach (var filter in filters)
        {
            if (!filter.Matches(item))
            {
                return false;
            }
        }
        if (words.Length == 0)
        {
            return true;
        }

        var texts = new string?[searched.Length];
        foreach (var word in words)
        {
            var found = false;
            for (var i = 0; i < searched.Length && !found; i++)
            {
                texts[i] ??= searched[i].GetText(item);
                found = texts[i]!.Contains(word, StringComparison.OrdinalIgnoreCase);
            }
            if (!found)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Splits on spaces, commas and semicolons; double quotes keep a value with spaces together.</summary>
    internal static List<string> Tokenize(string? text)
    {
        var tokens = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            return tokens;
        }

        var current = new StringBuilder();
        var quoted = false;
        void Flush()
        {
            var token = current.ToString().Trim();
            current.Clear();
            if (token.Length > 0 && !tokens.Contains(token, StringComparer.OrdinalIgnoreCase))
            {
                tokens.Add(token);
            }
        }

        foreach (var ch in text)
        {
            if (ch == '"')
            {
                Flush();
                quoted = !quoted;
            }
            else if (!quoted && (char.IsWhiteSpace(ch) || ch == ',' || ch == ';'))
            {
                Flush();
            }
            else
            {
                current.Append(ch);
            }
        }
        Flush();
        return tokens;
    }

    // ---------------- Filter box in the column header ----------------

    private static void OnFilterButtonChecked(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is ToggleButton { Name: "PART_FilterButton", TemplatedParent: DataGridColumnHeader { Column: { } column } header })
        {
            ((JetPackGrid)sender).LoadFilterOptions(column);
            header.Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                if (header.Template?.FindName("PART_FilterBox", header) is TextBox box)
                {
                    box.Focus();
                    box.SelectAll();
                }
            });
        }
    }

    private static void OnFilterBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Escape && e.OriginalSource is TextBox { Name: "PART_FilterBox", TemplatedParent: DataGridColumnHeader header })
        {
            ((JetPackGrid)sender).CloseFilter(header);
            e.Handled = true;
        }
    }

    private static void OnClearFilter(object sender, ExecutedRoutedEventArgs e)
    {
        var grid = (JetPackGrid)sender;
        var column = e.Parameter as DataGridColumn ?? (e.Parameter as DataGridColumnHeader)?.Column;
        if (column == null)
        {
            grid.ClearFilters();
            return;
        }
        column.SetCurrentValue(FilterTextProperty, string.Empty);
        grid.ApplyFilters();
    }

    private static void OnCloseFilter(object sender, ExecutedRoutedEventArgs e)
    {
        if (e.Parameter is DataGridColumnHeader header)
        {
            ((JetPackGrid)sender).CloseFilter(header);
        }
    }

    private void CloseFilter(DataGridColumnHeader header)
    {
        if (header.Template?.FindName("PART_FilterButton", header) is ToggleButton button)
        {
            button.IsChecked = false;
        }
        if (timer.IsEnabled)
        {
            ApplyFilters();
        }
        Keyboard.Focus(this);
    }

    // ---------------- Reading column values ----------------

    /// <summary>Reads a column's text for an item the way the cell shows it, without a cell.</summary>
    internal sealed class ColumnReader
    {
        private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> Properties = new();

        private readonly string[]? path;
        private readonly IValueConverter? converter;
        private readonly object? converterParameter;
        private readonly string? format;
        private readonly object? targetNullValue;
        private readonly CultureInfo culture;

        private ColumnReader(string[]? path, Binding? binding, CultureInfo culture)
        {
            this.path = path;
            this.culture = binding?.ConverterCulture ?? culture;
            converter = binding?.Converter;
            converterParameter = binding?.ConverterParameter;
            targetNullValue = binding?.TargetNullValue == DependencyProperty.UnsetValue ? null : binding?.TargetNullValue;
            var stringFormat = binding?.StringFormat;
            format = string.IsNullOrEmpty(stringFormat) ? null : stringFormat.Contains('{') ? stringFormat : "{0:" + stringFormat + "}";
        }

        public static ColumnReader? Create(DataGridColumn column, CultureInfo culture)
        {
            var memberPath = GetFilterMemberPath(column);
            if (!string.IsNullOrEmpty(memberPath))
            {
                return new ColumnReader(Split(memberPath), null, culture);
            }
            if (column is DataGridBoundColumn { Binding: Binding binding }
                && binding.Source == null && binding.RelativeSource == null && string.IsNullOrEmpty(binding.ElementName))
            {
                return new ColumnReader(Split(binding.Path?.Path), binding, culture);
            }
            return string.IsNullOrEmpty(column.SortMemberPath) ? null : new ColumnReader(Split(column.SortMemberPath), null, culture);
        }

        // An empty array reads the item itself; null means a path this reader does not follow (indexers, attached properties).
        private static string[]? Split(string? memberPath)
        {
            if (string.IsNullOrEmpty(memberPath) || memberPath == ".")
            {
                return Array.Empty<string>();
            }
            return memberPath.IndexOfAny(['[', '(', '/']) >= 0 ? null : memberPath.Split('.');
        }

        public string GetText(object item)
        {
            if (path == null)
            {
                return string.Empty;
            }

            object? value = item;
            foreach (var name in path)
            {
                if (value == null)
                {
                    break;
                }
                value = GetMember(value, name);
            }

            if (converter != null)
            {
                value = converter.Convert(value, typeof(string), converterParameter, culture);
            }
            if (value == null || value == DependencyProperty.UnsetValue)
            {
                value = targetNullValue;
            }
            if (value == null)
            {
                return string.Empty;
            }
            if (format != null)
            {
                return string.Format(culture, format, value);
            }
            return value is IFormattable formattable ? formattable.ToString(null, culture) : value.ToString() ?? string.Empty;
        }

        private static object? GetMember(object target, string name)
        {
            if (target is ICustomTypeDescriptor)
            {
                // DataRowView and other rows that describe their own columns.
                return TypeDescriptor.GetProperties(target)[name]?.GetValue(target);
            }
            var property = Properties.GetOrAdd((target.GetType(), name), static key =>
            {
                try
                {
                    return key.Item1.GetProperty(key.Item2, BindingFlags.Public | BindingFlags.Instance);
                }
                catch (AmbiguousMatchException)
                {
                    return key.Item1.GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(p => p.Name == key.Item2);
                }
            });
            return property != null && property.GetIndexParameters().Length == 0 ? property.GetValue(target) : null;
        }
    }

    private sealed class ColumnFilter(ColumnReader reader, FilterValue[] values)
    {
        public bool Matches(object item)
        {
            var text = reader.GetText(item).Trim();
            foreach (var value in values)
            {
                if (value.Matches(text))
                {
                    return true;
                }
            }
            return false;
        }
    }

    private readonly record struct FilterValue(string Text, bool AnyStart, bool AnyEnd)
    {
        public static FilterValue Parse(string token)
        {
            var anyStart = token.StartsWith('*');
            var anyEnd = token.EndsWith('*') && token.Length > 1;
            return new FilterValue(token.Trim('*'), anyStart, anyEnd);
        }

        public bool Matches(string text) => (AnyStart, AnyEnd) switch
        {
            _ when Text.Length == 0 => text.Length > 0,
            (true, true) => text.Contains(Text, StringComparison.OrdinalIgnoreCase),
            (true, false) => text.EndsWith(Text, StringComparison.OrdinalIgnoreCase),
            (false, true) => text.StartsWith(Text, StringComparison.OrdinalIgnoreCase),
            _ => text.Equals(Text, StringComparison.OrdinalIgnoreCase),
        };
    }
}
