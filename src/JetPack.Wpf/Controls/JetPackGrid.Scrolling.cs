using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace JetPack.Wpf.Controls;

// Dragging the scroll bar thumb: the scroll viewer defers the thumb's offset (IsDeferredScrollingEnabled
// in the template) and the rows the panel has realized stay where they are, while their cells show the
// texts of the rows at the thumb's position, read straight from the items. Moving a page of rows for
// real re-prepares every row (new item, DataContext and bindings in every cell, out of and back into
// the panel), which cannot keep up with a fast drag. Setting the texts of the cells that are already
// there costs a few milliseconds. On release the rows get their own texts back and the grid scrolls for
// real, in the same layout pass, so no frame shows the rows in between.
//
// Grouped grids, pixel scrolling and rows with details scroll live instead: their rows do not all have
// the same height, so the realized rows cannot stand in for the rows at the thumb.
public partial class JetPackGrid
{
    private ScrubPreview? scrub;

    private static void OnScrollBarScroll(object sender, ScrollEventArgs e)
    {
        var grid = (JetPackGrid)sender;
        if (e.OriginalSource is not ScrollBar { Orientation: Orientation.Vertical } bar
            || bar.TemplatedParent is not ScrollViewer { Name: "DG_ScrollViewer", IsDeferredScrollingEnabled: true } viewer)
        {
            return;
        }
        if (e.ScrollEventType == ScrollEventType.ThumbTrack)
        {
            if (!grid.ShowScrubPreview(viewer, e.NewValue))
            {
                viewer.ScrollToVerticalOffset(e.NewValue);
            }
        }
        else if (e.ScrollEventType == ScrollEventType.EndScroll)
        {
            grid.EndScrubPreview();
        }
    }

    /// <summary>Shows the rows at a thumb offset in the realized rows; false when the grid cannot (it then scrolls).</summary>
    private bool ShowScrubPreview(ScrollViewer viewer, double offset)
    {
        scrub ??= ScrubPreview.Create(this);
        if (scrub != null && scrub.Show(viewer, (int)offset))
        {
            return true;
        }
        EndScrubPreview();
        return false;
    }

    /// <summary>Gives the rows their own texts and places back.</summary>
    private void EndScrubPreview()
    {
        scrub?.Restore();
        scrub = null;
    }

    private sealed class ScrubPreview
    {
        // Far above the rows: where rows the preview does not use are put.
        private const double Away = -100_000;

        private readonly JetPackGrid grid;
        private readonly (DataGridColumn Column, ColumnReader? Reader)[] columns;
        private readonly HashSet<object> selected;
        private readonly HashSet<DataGridRow> rows = [];
        private readonly HashSet<DataGridRow> styled = [];
        private readonly Dictionary<FrameworkElement, object> hidden = [];
        private readonly Dictionary<DataGridRow, (object Local, TranslateTransform Move)> moved = [];
        private readonly List<(DataGridRow Row, double Top)> realized = [];

        private ScrubPreview(JetPackGrid grid, (DataGridColumn, ColumnReader?)[] columns)
        {
            this.grid = grid;
            this.columns = columns;
            selected = new HashSet<object>(grid.SelectedItems.Cast<object>(), ReferenceEqualityComparer.Instance);
        }

        public static ScrubPreview? Create(JetPackGrid grid)
        {
            if (grid.IsGrouping || VirtualizingPanel.GetScrollUnit(grid) != ScrollUnit.Item || (grid.RowDetailsVisibilityMode != DataGridRowDetailsVisibilityMode.Collapsed && (grid.RowDetailsTemplate != null || grid.RowDetailsTemplateSelector != null)))
            {
                return null;
            }
            var culture = grid.Language.GetSpecificCulture();
            var columns = grid.Columns
                .Where(c => c.Visibility == Visibility.Visible)
                .Select(c => (c, c is JetPackGridColumn text ? ColumnReader.ForCell(text, culture) : null))
                .ToArray();
            // A text column whose value cannot be read without its binding would show the wrong rows.
            return columns.Any(c => c.Item1 is JetPackGridColumn && c.Item2 == null) ? null : new ScrubPreview(grid, columns);
        }

        public bool Show(ScrollViewer viewer, int first)
        {
            if (viewer.Template?.FindName("PART_ScrollContentPresenter", viewer) is not ScrollContentPresenter presenter
                || FindRowsPanel(presenter) is not Panel panel)
            {
                return false;
            }

            // The realized rows and where the panel put them; the rows in view first, from the top, so
            // they stay in place.
            realized.Clear();
            var panelTop = panel.TranslatePoint(default, presenter).Y;
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(panel); i++)
            {
                if (VisualTreeHelper.GetChild(panel, i) is not DataGridRow row || grid.ItemContainerGenerator.IndexFromContainer(row) < 0)
                {
                    continue;
                }
                if (row.IsEditing)
                {
                    return false;
                }
                realized.Add((row, VisualTreeHelper.GetOffset(row).Y + panelTop));
            }
            if (realized.Count == 0)
            {
                return false;
            }
            realized.Sort((a, b) => a.Top < 0 != b.Top < 0 ? (a.Top < 0 ? 1 : -1) : a.Top.CompareTo(b.Top));

            // The rows at the thumb, top down, until the view is full.
            var rowHeight = realized[0].Row.ActualHeight;
            var height = presenter.ActualHeight;
            var items = grid.Items;
            var used = 0;
            var y = 0.0;
            for (var index = first; y < height && index < items.Count; index++)
            {
                var item = items[index];
                if (item == CollectionView.NewItemPlaceholder || used == realized.Count)
                {
                    return false;
                }
                var (row, top) = realized[used++];
                Move(row, y - top);
                ShowRow(row, item);
                y += rowHeight;
            }
            for (var i = used; i < realized.Count; i++)
            {
                Move(realized[i].Row, Away);
            }
            return true;
        }

        private static Panel? FindRowsPanel(DependencyObject root)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is DataGridRowsPresenter panel)
                {
                    return panel;
                }
                if (FindRowsPanel(child) is { } found)
                {
                    return found;
                }
            }
            return null;
        }

        // Moves a row by a distance from where the panel put it, without layout.
        private void Move(DataGridRow row, double distance)
        {
            if (moved.TryGetValue(row, out var entry))
            {
                entry.Move.Y = distance;
            }
            else if (distance != 0)
            {
                var move = new TranslateTransform(0, distance);
                moved[row] = (row.ReadLocalValue(RenderTransformProperty), move);
                row.RenderTransform = move;
            }
        }

        private void ShowRow(DataGridRow row, object item)
        {
            rows.Add(row);
            foreach (var (column, reader) in columns)
            {
                if (column.GetCellContent(row) is not { } content)
                {
                    continue;
                }
                var drawn = column is JetPackGridColumn text && reader != null && text.ShowPreview(content, reader.GetText(item));
                if (!drawn && !hidden.ContainsKey(content))
                {
                    // Content the grid cannot show for another item (other columns, templates) is left out until release.
                    hidden[content] = content.ReadLocalValue(OpacityProperty);
                    content.Opacity = 0;
                }
            }

            // The row's triggers show the selection of its own item; the previewed item's is set here, as
            // the row template (JetPack.Styles.DataGridRow) shows it. The rows keep their alternating
            // backgrounds: after a far jump WPF numbers them from the top row again, not by item index.
            var isSelected = selected.Contains(item);
            if (isSelected == row.IsSelected)
            {
                Unstyle(row);
                return;
            }
            if (row.Template?.FindName("DGR_Border", row) is not Border border)
            {
                return;
            }
            styled.Add(row);
            if (isSelected)
            {
                border.SetResourceReference(Border.BackgroundProperty, "JetPack.Brushes.Accent.Selection");
                // The text colour WPF's theme gives selected cells, which the cells here inherit instead.
                row.Foreground = (bool)row.GetValue(Selector.IsSelectionActiveProperty)
                    ? SystemColors.HighlightTextBrush
                    : SystemColors.InactiveSelectionHighlightTextBrush;
                return;
            }
            border.Background = row.Background;
            // The colour the row inherits when its triggers do not set one; the cells are still selected, so
            // the theme's selection colour is replaced on them too.
            var foreground = VisualTreeHelper.GetParent(row)?.GetValue(TextElement.ForegroundProperty) as Brush ?? grid.Foreground;
            row.Foreground = foreground;
            foreach (var (column, _) in columns)
            {
                if (column.GetCellContent(row)?.Parent is DataGridCell cell)
                {
                    cell.Foreground = foreground;
                }
            }
        }

        private void Unstyle(DataGridRow row)
        {
            if (styled.Remove(row))
            {
                (row.Template?.FindName("DGR_Border", row) as Border)?.ClearValue(Border.BackgroundProperty);
                row.ClearValue(ForegroundProperty);
                foreach (var (column, _) in columns)
                {
                    (column.GetCellContent(row)?.Parent as DataGridCell)?.ClearValue(ForegroundProperty);
                }
            }
        }

        public void Restore()
        {
            foreach (var row in rows)
            {
                foreach (var (column, _) in columns)
                {
                    if (column is JetPackGridColumn text && column.GetCellContent(row) is { } content)
                    {
                        text.RestoreText(content);
                    }
                }
                Unstyle(row);
            }
            foreach (var (content, opacity) in hidden)
            {
                Reset(content, OpacityProperty, opacity);
            }
            foreach (var (row, (local, _)) in moved)
            {
                Reset(row, RenderTransformProperty, local);
            }
        }

        private static void Reset(DependencyObject target, DependencyProperty property, object local)
        {
            if (local == DependencyProperty.UnsetValue)
            {
                target.ClearValue(property);
            }
            else
            {
                target.SetValue(property, local);
            }
        }
    }
}
