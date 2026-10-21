using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace JetPack.Wpf.Controls;

/// <summary>
/// Text column for <see cref="JetPackGrid"/>: shows its Binding like <see cref="DataGridTextColumn"/>
/// (and edits it the same way) and marks the words of the grid's search in the cell.
/// </summary>
public class JetPackGridColumn : DataGridTextColumn
{
    public static readonly DependencyProperty IsSearchableProperty = JetPackGrid.IsSearchableProperty.AddOwner(typeof(JetPackGridColumn));

    /// <summary>Whether the grid's search looks in this column and marks its hits.</summary>
    public bool IsSearchable
    {
        get => (bool)GetValue(IsSearchableProperty);
        set => SetValue(IsSearchableProperty, value);
    }

    public static readonly DependencyProperty IsFilterableProperty = JetPackGrid.IsFilterableProperty.AddOwner(typeof(JetPackGridColumn));

    /// <summary>Whether the header shows the filter button.</summary>
    public bool IsFilterable
    {
        get => (bool)GetValue(IsFilterableProperty);
        set => SetValue(IsFilterableProperty, value);
    }

    public static readonly DependencyProperty FilterMemberPathProperty = JetPackGrid.FilterMemberPathProperty.AddOwner(typeof(JetPackGridColumn));

    /// <summary>Member the search and filter read instead of the Binding.</summary>
    public string? FilterMemberPath
    {
        get => (string?)GetValue(FilterMemberPathProperty);
        set => SetValue(FilterMemberPathProperty, value);
    }

    public static readonly DependencyProperty FilterTextProperty = JetPackGrid.FilterTextProperty.AddOwner(typeof(JetPackGridColumn));

    /// <summary>Values this column must equal, separated by spaces or commas (see <see cref="JetPackGrid.GetFilterText"/>).</summary>
    public string FilterText
    {
        get => (string)GetValue(FilterTextProperty);
        set => SetValue(FilterTextProperty, value);
    }

    // The cell's text, its column and the grid's search words; any change rebuilds the marked runs.
    private static readonly DependencyProperty CellTextProperty = DependencyProperty.RegisterAttached(
        "CellText", typeof(string), typeof(JetPackGridColumn), new PropertyMetadata(null, OnCellChanged));

    private static readonly DependencyProperty WordsProperty = DependencyProperty.RegisterAttached(
        "Words", typeof(IReadOnlyList<string>), typeof(JetPackGridColumn), new PropertyMetadata(null, OnCellChanged));

    private static readonly DependencyProperty ColumnProperty = DependencyProperty.RegisterAttached(
        "Column", typeof(JetPackGridColumn), typeof(JetPackGridColumn), new PropertyMetadata(null));

    private static readonly DependencyProperty IsMarkedProperty = DependencyProperty.RegisterAttached(
        "IsMarked", typeof(bool), typeof(JetPackGridColumn), new PropertyMetadata(false));

    protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
    {
        // The base element carries the ElementStyle and the column's font settings; only its text is ours.
        var block = (TextBlock)base.GenerateElement(cell, dataItem);
        BindingOperations.ClearBinding(block, TextBlock.TextProperty);
        block.SetValue(ColumnProperty, this);
        if (DataGridOwner is JetPackGrid grid)
        {
            BindingOperations.SetBinding(block, WordsProperty, new Binding(nameof(JetPackGrid.SearchWords)) { Source = grid });
        }
        if (Binding != null)
        {
            BindingOperations.SetBinding(block, CellTextProperty, Binding);
        }
        return block;
    }

    private static void OnCellChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var block = (TextBlock)d;
        var text = (string?)block.GetValue(CellTextProperty) ?? string.Empty;
        var words = block.GetValue(WordsProperty) as IReadOnlyList<string>;
        var column = block.GetValue(ColumnProperty) as JetPackGridColumn;
        var hits = words is { Count: > 0 } && column is { IsSearchable: true } ? FindHits(text, words) : null;
        if (hits == null)
        {
            if ((bool)block.GetValue(IsMarkedProperty))
            {
                // Text may still hold the same string from before the runs, so setting it alone would not replace them.
                block.ClearValue(IsMarkedProperty);
                block.ClearValue(TextBlock.TextProperty);
            }
            block.Text = text;
            return;
        }

        var grid = column!.DataGridOwner;
        block.SetValue(IsMarkedProperty, true);
        block.Inlines.Clear();
        var position = 0;
        foreach (var (start, end) in hits)
        {
            if (start > position)
            {
                block.Inlines.Add(new Run(text[position..start]));
            }
            var run = new Run(text[start..end]);
            if (grid is JetPackGrid { HighlightBrush: var background, HighlightForeground: var foreground })
            {
                run.Background = background;
                if (foreground != null)
                {
                    run.Foreground = foreground;
                }
            }
            block.Inlines.Add(run);
            position = end;
        }
        if (position < text.Length)
        {
            block.Inlines.Add(new Run(text[position..]));
        }
    }

    /// <summary>Ranges (start, end) of every occurrence of the words, ignoring case, merged where they overlap.</summary>
    private static List<(int Start, int End)>? FindHits(string text, IReadOnlyList<string> words)
    {
        List<(int Start, int End)>? hits = null;
        foreach (var word in words)
        {
            for (var index = text.IndexOf(word, StringComparison.OrdinalIgnoreCase); index >= 0;
                 index = text.IndexOf(word, index + word.Length, StringComparison.OrdinalIgnoreCase))
            {
                (hits ??= new()).Add((index, index + word.Length));
            }
        }
        if (hits == null)
        {
            return null;
        }

        hits.Sort();
        var merged = new List<(int Start, int End)> { hits[0] };
        foreach (var hit in hits.Skip(1))
        {
            var last = merged[^1];
            if (hit.Start <= last.End)
            {
                merged[^1] = (last.Start, Math.Max(last.End, hit.End));
            }
            else
            {
                merged.Add(hit);
            }
        }
        return merged;
    }
}
