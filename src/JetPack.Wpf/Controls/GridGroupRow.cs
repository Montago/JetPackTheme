using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace JetPack.Wpf.Controls;

/// <summary>
/// A group header in a grouped <see cref="JetPackGrid"/>. The grid shows groups as rows of their own in
/// its flat list of rows (no CollectionView groups), so a grouped grid virtualizes and scrolls like an
/// ungrouped one. This is the DataContext of the header (<c>JetPack.Styles.JetPackGrid.GroupHeader</c>).
/// </summary>
/// <remarks>
/// While the grid is grouped, <c>Items</c> holds these rows between the data rows; use
/// <see cref="JetPackGrid.GetVisibleItems"/> for the data rows alone. They are never selected.
/// </remarks>
public sealed class GridGroupRow : INotifyPropertyChanged
{
    private readonly JetPackGrid grid;
    private bool isExpanded;

    internal GridGroupRow(JetPackGrid grid, object? name, IReadOnlyList<object> items, bool isExpanded)
    {
        this.grid = grid;
        Name = name;
        Items = items;
        this.isExpanded = isExpanded;
    }

    /// <summary>The value the rows of the group share.</summary>
    public object? Name { get; }

    /// <summary>The data rows of the group on the page shown, folded or not.</summary>
    public IReadOnlyList<object> Items { get; }

    /// <summary>Number of rows in the group on the page shown.</summary>
    public int ItemCount => Items.Count;

    /// <summary>Whether the group's rows are shown; false folds the group up to its header.</summary>
    public bool IsExpanded
    {
        get => isExpanded;
        set
        {
            if (isExpanded == value)
            {
                return;
            }
            isExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
            grid.OnGroupToggled(this);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public override string ToString() => Name?.ToString() ?? string.Empty;
}

/// <summary>
/// The rows a grouped <see cref="JetPackGrid"/> hands the DataGrid in place of the app's collection:
/// headers and data rows in the order shown. Read-only, so the DataGrid neither adds nor deletes rows.
/// </summary>
internal sealed class GroupedRows(IEnumerable source) : IList, INotifyCollectionChanged
{
    private static readonly NotifyCollectionChangedEventArgs ResetArgs = new(NotifyCollectionChangedAction.Reset);

    private List<object> rows = [];
    // Position of each data row in its group, by row; the rows alternate their background within a group.
    private Dictionary<object, int> positions = new(ReferenceEqualityComparer.Instance);

    /// <summary>The collection the app set as ItemsSource.</summary>
    public IEnumerable Source { get; } = source;

    public void Reset(List<object> shown)
    {
        rows = shown;
        positions = new Dictionary<object, int>(shown.Count, ReferenceEqualityComparer.Instance);
        var position = 0;
        foreach (var row in shown)
        {
            if (row is GridGroupRow)
            {
                position = 0;
            }
            else
            {
                positions.TryAdd(row, position++);
            }
        }
        CollectionChanged?.Invoke(this, ResetArgs);
    }

    /// <summary>True when the data row is an odd one in its group (it gets the alternating background).</summary>
    public bool IsAlternate(object row) => positions.TryGetValue(row, out var position) && position % 2 == 1;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public object? this[int index] { get => rows[index]; set => throw new NotSupportedException(); }
    public int Count => rows.Count;
    public bool IsReadOnly => true;
    public bool IsFixedSize => true;
    bool ICollection.IsSynchronized => false;
    object ICollection.SyncRoot => this;
    public int IndexOf(object? value) => value == null ? -1 : rows.IndexOf(value);
    public bool Contains(object? value) => IndexOf(value) >= 0;
    public IEnumerator GetEnumerator() => rows.GetEnumerator();
    void ICollection.CopyTo(Array array, int index) => ((ICollection)rows).CopyTo(array, index);
    int IList.Add(object? value) => throw new NotSupportedException();
    void IList.Clear() => throw new NotSupportedException();
    void IList.Insert(int index, object? value) => throw new NotSupportedException();
    void IList.Remove(object? value) => throw new NotSupportedException();
    void IList.RemoveAt(int index) => throw new NotSupportedException();
}
