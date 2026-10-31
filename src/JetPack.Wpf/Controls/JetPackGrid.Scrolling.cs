using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
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
// Grouped, the rows at the thumb have group headers where the realized rows may have data rows and the
// other way round, and headers are not as tall. The data rows (those in view and the panel's cache above
// and below) are moved to their places with a render transform, which needs no layout, and the headers
// are drawn by a few header toggles on PART_ScrubLayer over the rows.
//
// Pixel scrolling, rows with details and CollectionView groups (Items.GroupDescriptions set by the app)
// scroll live instead: their rows do not all have the same height, so the realized rows cannot stand in
// for the rows at the thumb.
//
// The horizontal thumb is deferred too (the setting covers both bars), but columns are cheap to bring in,
// so the grid scrolls sideways live while it is dragged.
public partial class JetPackGrid
{
    private ScrubPreview? scrub;

    private static void OnScrollBarScroll(object sender, ScrollEventArgs e)
    {
        var grid = (JetPackGrid)sender;
        if (e.OriginalSource is not ScrollBar bar
            || bar.TemplatedParent is not ScrollViewer { Name: "DG_ScrollViewer", IsDeferredScrollingEnabled: true } viewer)
        {
            return;
        }
        if (bar.Orientation == Orientation.Horizontal)
        {
            // The deferral is meant for the rows; the columns follow the thumb live.
            if (e.ScrollEventType == ScrollEventType.ThumbTrack)
            {
                viewer.ScrollToHorizontalOffset(e.NewValue);
            }
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
        private readonly Dictionary<DataGridCell, object> framed = [];
        // The cell with keyboard focus, which shows the focus frame (JetPack.Styles.DataGridCell), and its item.
        private readonly DataGridCell? focused;
        private readonly object? focusedItem;
        private readonly Dictionary<DataGridRow, (object Local, TranslateTransform Move)> moved = [];
        private readonly List<ToggleButton> headers = [];
        private readonly List<(DataGridRow Row, double Top)> dataRows = [];
        private readonly List<DataGridRow> headerRows = [];
        private Canvas? layer;

        private ScrubPreview(JetPackGrid grid, (DataGridColumn, ColumnReader?)[] columns)
        {
            this.grid = grid;
            this.columns = columns;
            selected = new HashSet<object>(grid.SelectedItems.Cast<object>(), ReferenceEqualityComparer.Instance);
            for (var element = Keyboard.FocusedElement as DependencyObject; element != null && element != grid; element = VisualTreeHelper.GetParent(element))
            {
                if (element is DataGridCell { IsKeyboardFocusWithin: true } cell)
                {
                    focused = cell;
                    focusedItem = cell.DataContext;
                    break;
                }
            }
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
                .Select(c => (c, c is JetPackGridColumn or JetPackGridCheckBoxColumn ? ColumnReader.ForCell((DataGridBoundColumn)c, culture) : null))
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
            // they stay in place where they can.
            dataRows.Clear();
            headerRows.Clear();
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
                if (row.Item is GridGroupRow)
                {
                    headerRows.Add(row);
                }
                else
                {
                    dataRows.Add((row, VisualTreeHelper.GetOffset(row).Y + panelTop));
                }
            }
            if (dataRows.Count == 0)
            {
                return false;
            }
            dataRows.Sort((a, b) => a.Top < 0 != b.Top < 0 ? (a.Top < 0 ? 1 : -1) : a.Top.CompareTo(b.Top));

            // The rows at the thumb, top down, until the view is full.
            var rowHeight = dataRows[0].Row.ActualHeight;
            var headerHeight = headerRows.Count > 0 ? headerRows[0].ActualHeight : 0;
            var height = presenter.ActualHeight;
            var items = grid.Items;
            var used = 0;
            var usedHeaders = 0;
            var y = 0.0;
            for (var index = first; y < height && index < items.Count; index++)
            {
                var item = items[index];
                if (item == CollectionView.NewItemPlaceholder)
                {
                    return false;
                }
                if (item is GridGroupRow group)
                {
                    if (Header(usedHeaders++, viewer) is not { } header)
                    {
                        return false;
                    }
                    header.DataContext = group;
                    header.Width = presenter.ActualWidth;
                    header.Visibility = Visibility.Visible;
                    Canvas.SetTop(header, y);
                    if (headerHeight <= 0)
                    {
                        header.Measure(new Size(presenter.ActualWidth, double.PositiveInfinity));
                        headerHeight = header.DesiredSize.Height;
                    }
                    y += headerHeight;
                    continue;
                }
                if (used == dataRows.Count)
                {
                    return false;
                }
                var (row, top) = dataRows[used++];
                Move(row, y - top);
                ShowRow(row, item);
                y += rowHeight;
            }

            for (var i = used; i < dataRows.Count; i++)
            {
                Move(dataRows[i].Row, Away);
            }
            foreach (var row in headerRows)
            {
                Move(row, Away);
            }
            for (var i = usedHeaders; i < headers.Count; i++)
            {
                headers[i].Visibility = Visibility.Collapsed;
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

        // A group header like the one in the group row template, on the layer over the rows.
        private ToggleButton? Header(int index, ScrollViewer viewer)
        {
            if (index < headers.Count)
            {
                return headers[index];
            }
            layer ??= viewer.Template.FindName("PART_ScrubLayer", viewer) as Canvas;
            if (layer == null)
            {
                return null;
            }
            var header = new ToggleButton { HorizontalAlignment = HorizontalAlignment.Left };
            header.SetResourceReference(StyleProperty, "JetPack.Styles.JetPackGrid.GroupHeader");
            header.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(GridGroupRow.IsExpanded)) { Mode = BindingMode.OneWay });
            layer.Children.Add(header);
            headers.Add(header);
            return header;
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
                var drawn = reader != null && column switch
                {
                    JetPackGridColumn text => text.ShowPreview(content, reader.GetText(item)),
                    JetPackGridCheckBoxColumn => JetPackGridCheckBoxColumn.ShowPreview(content, reader.GetValue(item)),
                    _ => false,
                };
                if (!drawn && !hidden.ContainsKey(content))
                {
                    // Content the grid cannot show for another item (other columns, templates) is left out until release.
                    hidden[content] = content.ReadLocalValue(OpacityProperty);
                    content.Opacity = 0;
                }
                if (content.Parent is DataGridCell cell)
                {
                    ShowFocus(cell, focused != null && ReferenceEquals(item, focusedItem) && column == focused.Column);
                }
            }

            // The row's triggers show the selection of its own item; the previewed item's is set here, as
            // the row template (JetPack.Styles.DataGridRow) shows it. Ungrouped, the rows keep their
            // alternating backgrounds: after a far jump WPF numbers them from the top row again, not by item
            // index. Grouped, the rows alternate within each group, so the previewed item's background is set.
            var isSelected = selected.Contains(item);
            var background = grid.GroupedRowBackground(item) ?? row.Background;
            if (isSelected == row.IsSelected && (isSelected || ReferenceEquals(background, row.Background)))
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
            border.Background = background;
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

        // The focus frame belongs on the cell that shows the focused cell's item, not on the focused cell.
        private void ShowFocus(DataGridCell cell, bool frame)
        {
            if (frame == ReferenceEquals(cell, focused))
            {
                if (framed.Remove(cell, out var local))
                {
                    Reset(cell, BorderBrushProperty, local);
                }
                return;
            }
            framed.TryAdd(cell, cell.ReadLocalValue(BorderBrushProperty));
            if (frame)
            {
                cell.SetResourceReference(BorderBrushProperty, "JetPack.Brushes.Focus");
            }
            else
            {
                cell.BorderBrush = Brushes.Transparent;
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
                    switch (column, column.GetCellContent(row))
                    {
                        case (JetPackGridColumn text, { } content):
                            text.RestoreText(content);
                            break;
                        case (JetPackGridCheckBoxColumn, { } content):
                            JetPackGridCheckBoxColumn.RestoreValue(content);
                            break;
                    }
                }
                Unstyle(row);
            }
            foreach (var (content, opacity) in hidden)
            {
                Reset(content, OpacityProperty, opacity);
            }
            foreach (var (cell, brush) in framed)
            {
                Reset(cell, BorderBrushProperty, brush);
            }
            foreach (var (row, (local, _)) in moved)
            {
                Reset(row, RenderTransformProperty, local);
            }
            foreach (var header in headers)
            {
                layer?.Children.Remove(header);
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
