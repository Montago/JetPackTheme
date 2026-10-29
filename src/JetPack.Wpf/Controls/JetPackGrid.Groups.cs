using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace JetPack.Wpf.Controls;

// Flat grouping: while GroupMemberPath is set, the DataGrid gets a list of the grid's own in place of the
// app's collection (ItemsSource is coerced to it), with a header row (GridGroupRow) in front of each
// group's rows. Folded groups leave their rows out. WPF's own grouping (CollectionView groups, a GroupItem
// with its own panel per group) needs pixel scrolling, as headers and rows differ in height, and builds
// rows for every group it passes while measuring: a far jump cost about 120 ms. In one flat list the
// rows virtualize and scroll by item as without groups, and the thumb-drag preview covers them.
//
// The grid sorts and filters the rows itself while grouped: the DataGrid's view of the flat list is never
// sorted or filtered (that would sort the headers in with the rows), so the sort lives in groupSorts.
public partial class JetPackGrid
{
    /// <summary>Key of the template of the group header rows (a <see cref="DataGridRow"/> template).</summary>
    public const string GroupRowTemplateKey = "JetPack.Templates.JetPackGrid.GroupRow";

    // The name of groups whose value is null, in the set of folded groups.
    private static readonly object NullName = new();

    private static CoerceValueCallback? dataGridCoerceItemsSource;

    // The flat list the DataGrid shows while grouped; null when not grouped.
    private GroupedRows? grouped;
    // While grouped: the groups of the rows that pass the filters, as runs of `ordered`.
    private readonly List<(object? Name, int Start, int Count)> groupRuns = [];
    private readonly HashSet<object> foldedGroups = [];
    private readonly List<SortDescription> groupSorts = [];
    private bool switchingGrouping;
    private bool regroupPending;
    private ControlTemplate? groupRowTemplate;
    private Binding? rowBackground;
    private Binding? alternatingBackground;

    /// <summary>True while <see cref="GroupMemberPath"/> groups the rows (and <c>Items</c> holds <see cref="GridGroupRow"/> headers).</summary>
    public bool IsGrouped => grouped != null;

    /// <summary>Unfolds every group.</summary>
    public void ExpandAllGroups() => SetAllGroups(expanded: true);

    /// <summary>Folds every group up to its header.</summary>
    public void CollapseAllGroups() => SetAllGroups(expanded: false);

    // The collection the app set as ItemsSource (ItemsSource itself is the flat list while grouped).
    private IEnumerable Source => grouped?.Source ?? Items.SourceCollection;

    // The sort the rows are shown in.
    private IList<SortDescription> Sorts => grouped != null ? groupSorts : Items.SortDescriptions;

    private static object? CoerceItemsSource(DependencyObject d, object? baseValue)
    {
        var grid = (JetPackGrid)d;
        var value = baseValue;
        if (baseValue is IEnumerable source && !string.IsNullOrEmpty(grid.GroupMemberPath))
        {
            if (grid.grouped?.Source != source)
            {
                grid.grouped = new GroupedRows(source);
            }
            value = grid.grouped;
        }
        else
        {
            grid.grouped = null;
        }
        // The DataGrid's own coercion clears the sort when the items change.
        dataGridCoerceItemsSource ??= ItemsSourceProperty.GetMetadata(typeof(DataGrid)).CoerceValueCallback ?? ((_, v) => v);
        return dataGridCoerceItemsSource(d, value);
    }

    private void UpdateGrouping()
    {
        foldedGroups.Clear();
        var sorts = Sorts.ToList();
        var wasGrouped = grouped != null;
        switchingGrouping = true;
        try
        {
            CoerceValue(ItemsSourceProperty);
            if (wasGrouped != (grouped != null))
            {
                // The sort carries over from the app's view to the grid's and back.
                groupSorts.Clear();
                if (grouped != null)
                {
                    groupSorts.AddRange(sorts);
                }
                else
                {
                    using (Items.DeferRefresh())
                    {
                        Items.SortDescriptions.Clear();
                        foreach (var sort in sorts)
                        {
                            Items.SortDescriptions.Add(sort);
                        }
                    }
                }
                UpdateSortArrows();
            }
        }
        finally
        {
            switchingGrouping = false;
        }
        ApplyFilters();
    }

    /// <summary>
    /// A sort given to <c>Items.SortDescriptions</c> while grouped (by code: header clicks are handled in
    /// OnSorting) would sort the headers in with the rows; it becomes the grid's sort instead.
    /// </summary>
    private void OnSortDescriptionsChanged()
    {
        if (grouped == null || switchingGrouping || Items.SortDescriptions.Count == 0)
        {
            ScheduleRepage(keepPage: false);
            return;
        }
        Dispatcher.BeginInvoke(DispatcherPriority.DataBind, () =>
        {
            if (grouped == null || Items.SortDescriptions.Count == 0)
            {
                return;
            }
            groupSorts.Clear();
            groupSorts.AddRange(Items.SortDescriptions);
            switchingGrouping = true;
            try
            {
                Items.SortDescriptions.Clear();
            }
            finally
            {
                switchingGrouping = false;
            }
            UpdateSortArrows();
            Refilter(keepPage: false);
        });
    }

    /// <summary>Clicking a header while grouped: the same three states as without groups, in the grid's own sort.</summary>
    private void SortGrouped(DataGridColumn column)
    {
        var path = column.SortMemberPath;
        if (string.IsNullOrEmpty(path))
        {
            return;
        }
        var index = groupSorts.FindIndex(s => s.PropertyName == path);
        ListSortDirection? next = index < 0 ? ListSortDirection.Ascending
            : groupSorts[index].Direction == ListSortDirection.Ascending ? ListSortDirection.Descending
            : null;
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift)
        {
            groupSorts.Clear();
            if (next != null)
            {
                groupSorts.Add(new SortDescription(path, next.Value));
            }
        }
        else if (index >= 0)
        {
            if (next == null)
            {
                groupSorts.RemoveAt(index);
            }
            else
            {
                groupSorts[index] = new SortDescription(path, next.Value);
            }
        }
        else
        {
            groupSorts.Add(new SortDescription(path, next!.Value));
        }
        UpdateSortArrows();
        Refilter(keepPage: false);
    }

    private void UpdateSortArrows()
    {
        foreach (var column in Columns)
        {
            var path = column.SortMemberPath;
            ListSortDirection? direction = null;
            foreach (var sort in Sorts)
            {
                if (!string.IsNullOrEmpty(path) && sort.PropertyName == path)
                {
                    direction = sort.Direction;
                    break;
                }
            }
            if (column.SortDirection != direction)
            {
                column.SortDirection = direction;
            }
        }
    }

    private void SetAllGroups(bool expanded)
    {
        foldedGroups.Clear();
        if (!expanded)
        {
            foreach (var (name, _, _) in groupRuns)
            {
                foldedGroups.Add(name ?? NullName);
            }
        }
        ShowGroups();
    }

    internal void OnGroupToggled(GridGroupRow group)
    {
        var name = group.Name ?? NullName;
        if (group.IsExpanded)
        {
            foldedGroups.Remove(name);
        }
        else
        {
            foldedGroups.Add(name);
        }
        // Not inside the header's click: the rows are handed out again.
        if (!regroupPending)
        {
            regroupPending = true;
            Dispatcher.BeginInvoke(DispatcherPriority.DataBind, () =>
            {
                regroupPending = false;
                ShowGroups();
            });
        }
    }

    private void ShowGroups()
    {
        if (grouped != null)
        {
            ShowPage(PageIndex, scrollToTop: false);
        }
    }

    /// <summary>
    /// The rows with each group's rows together, the groups in the order of their first row (as CollectionView
    /// groups are), and the groups as runs of the result.
    /// </summary>
    private List<object> SplitIntoGroups(List<object> rows, List<(object? Name, int Start, int Count)> runs)
    {
        var description = new PropertyGroupDescription(GroupMemberPath);
        var culture = Language.GetSpecificCulture();
        var read = ColumnReader.ForValue(GroupMemberPath) ?? (item => description.GroupNameFromItem(item, 0, culture));
        var index = new Dictionary<object, int>();
        var groups = new List<(object? Name, List<object> Rows)>();
        foreach (var row in rows)
        {
            var name = read(row);
            if (!index.TryGetValue(name ?? NullName, out var i))
            {
                i = groups.Count;
                index[name ?? NullName] = i;
                groups.Add((name, []));
            }
            groups[i].Rows.Add(row);
        }

        var result = new List<object>(rows.Count);
        foreach (var (name, members) in groups)
        {
            runs.Add((name, result.Count, members.Count));
            result.AddRange(members);
        }
        return result;
    }

    /// <summary>The rows from <paramref name="start"/> of <c>ordered</c>, with a header in front of each group's part of them.</summary>
    private List<object> GroupRows(int start, int count)
    {
        var rows = new List<object>(count + groupRuns.Count);
        var end = start + count;
        foreach (var (name, runStart, runCount) in groupRuns)
        {
            var from = Math.Max(start, runStart);
            var to = Math.Min(end, runStart + runCount);
            if (from >= to)
            {
                continue;
            }
            var items = ordered!.GetRange(from, to - from);
            var expanded = !foldedGroups.Contains(name ?? NullName);
            rows.Add(new GridGroupRow(this, name, items, expanded));
            if (expanded)
            {
                rows.AddRange(items);
            }
        }
        return rows;
    }

    // Recycled rows are prepared again for their new item, which may be a header (a row without cells)
    // or a data row. The template is set before the DataGrid prepares the row, so a header never gets cells.
    private void PrepareGroupRow(DataGridRow row, object item)
    {
        if (item is GridGroupRow)
        {
            row.Template = groupRowTemplate ??= TryFindResource(GroupRowTemplateKey) as ControlTemplate;
            row.Focusable = false;
        }
        else if (row.ReadLocalValue(TemplateProperty) != DependencyProperty.UnsetValue)
        {
            row.ClearValue(TemplateProperty);
            row.ClearValue(FocusableProperty);
        }
    }

    // The rows alternate their background within each group, as the rows of a GroupItem did; WPF's own
    // alternation would count the headers.
    private void SetRowBackground(DataGridRow row, object item)
    {
        if (grouped != null)
        {
            row.SetBinding(BackgroundProperty, grouped.IsAlternate(item) ? alternatingBackground ??= new Binding(nameof(AlternatingRowBackground)) { Source = this }
                : rowBackground ??= new Binding(nameof(RowBackground)) { Source = this });
        }
        else if (row.ReadLocalValue(BackgroundProperty) is BindingExpression)
        {
            row.ClearValue(BackgroundProperty);
        }
    }

    /// <summary>The background a data row shows in a grouped grid; null when not grouped.</summary>
    private Brush? GroupedRowBackground(object item) => grouped == null ? null : grouped.IsAlternate(item) ? AlternatingRowBackground : RowBackground;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (grouped != null && SkipHeaders(e.Key))
        {
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    /// <summary>
    /// The DataGrid moves the current cell from row to row, and stops when the next row is a header, which
    /// has no cells. When a key would land on a header, the current cell goes to the next data row instead.
    /// </summary>
    private bool SkipHeaders(Key key)
    {
        if (CurrentCell.Item is not { } current || CurrentCell.Column is not { } column || Items.IndexOf(current) is not (>= 0 and var index))
        {
            return false;
        }
        var modifiers = Keyboard.Modifiers;
        var ctrl = (modifiers & ModifierKeys.Control) != 0;
        // The DataGrid pages by the rows in view, less one.
        var page = Math.Max(1, (int)((GetTemplateChild("DG_ScrollViewer") as ScrollViewer)?.ViewportHeight ?? 1) - 1);
        var (target, step) = key switch
        {
            Key.Down when !ctrl => (index + 1, 1),
            Key.Up when !ctrl => (index - 1, -1),
            Key.Down or Key.End when ctrl => (Items.Count - 1, -1),
            Key.Up or Key.Home when ctrl => (0, 1),
            Key.PageDown => (Math.Min(Items.Count - 1, index + page), 1),
            Key.PageUp => (Math.Max(0, index - page), -1),
            _ => (-1, 0),
        };
        if (target < 0 || target >= Items.Count || Items[target] is not GridGroupRow)
        {
            return false;
        }

        var found = target;
        while (found >= 0 && found < Items.Count && Items[found] is GridGroupRow)
        {
            found += step;
        }
        if (found < 0 || found >= Items.Count)
        {
            // Only headers that way (folded groups at the end): stay.
            return true;
        }

        var item = Items[found];
        var cell = new DataGridCellInfo(item, column);
        ScrollIntoView(item, column);
        CurrentCell = cell;
        var extend = (modifiers & ModifierKeys.Shift) != 0 && SelectionMode == DataGridSelectionMode.Extended;
        if (SelectionUnit == DataGridSelectionUnit.Cell)
        {
            if (!extend)
            {
                SelectedCells.Clear();
            }
            SelectedCells.Add(cell);
        }
        else if (extend)
        {
            SelectedItems.Add(item);
        }
        else
        {
            SetCurrentValue(SelectedItemProperty, item);
        }
        UpdateLayout();
        (column.GetCellContent(item)?.Parent as DataGridCell)?.Focus();
        return true;
    }

    // Ctrl+A or Shift+click across a group select the headers too.
    private bool DeselectHeaders(SelectionChangedEventArgs e)
    {
        if (grouped == null || e.AddedItems.Count == 0 || e.AddedItems.OfType<GridGroupRow>().ToList() is not { Count: > 0 } headers)
        {
            return false;
        }
        if (SelectionMode == DataGridSelectionMode.Single || SelectionUnit != DataGridSelectionUnit.FullRow)
        {
            SetCurrentValue(SelectedItemProperty, null);
        }
        else
        {
            foreach (var header in headers)
            {
                SelectedItems.Remove(header);
            }
        }
        return true;
    }
}
