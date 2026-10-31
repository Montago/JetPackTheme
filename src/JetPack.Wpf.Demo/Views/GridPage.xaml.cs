using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JetPack.Wpf.Controls;

namespace JetPack.Wpf.Demo.Views;

public partial class GridPage : UserControl
{
    public GridPage()
    {
        InitializeComponent();
        Cars.ApplyFilters();
        OnSelectedItemChanged(Cars, new RoutedPropertyChangedEventArgs<object?>(null, null));
    }

    private void OnFiltered(object sender, GridFilteredEventArgs e)
    {
        if (Status != null)
        {
            Status.Text = $"Showing {e.Count:N0} of {e.TotalCount:N0} cars";
        }
    }

    private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object?> e)
    {
        if (Selection != null)
        {
            Selection.Text = e.NewValue is Car car ? $"Selected: {car.StockNo}, {car.Make} {car.Model} {car.Year}" : "Nothing selected";
        }
    }

    private void OnMultiChanged(object sender, RoutedEventArgs e)
    {
        Cars.SelectionMode = Multi.IsChecked == true ? DataGridSelectionMode.Extended : DataGridSelectionMode.Single;
    }

    private void OnMarksChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Cars == null)
        {
            return;
        }
        // The grid's style gives the accent and the grey; local values override them.
        Cars.ClearValue(JetPackGrid.HighlightForegroundProperty);
        Cars.ClearValue(JetPackGrid.HighlightCellBackgroundProperty);
        switch (Marks.SelectedIndex)
        {
            case 1:
                Cars.HighlightCellBackground = null;
                break;
            case 2:
                Cars.HighlightForeground = Brushes.Firebrick;
                break;
        }
    }

    private void OnCopyVisible(object sender, RoutedEventArgs e)
    {
        // GetVisibleItems gives the rows left after the search and filters, in the order shown.
        var text = new StringBuilder("Stock no.\tMake\tModel\tYear\tPrice\n");
        foreach (Car car in Cars.GetVisibleItems())
        {
            text.Append($"{car.StockNo}\t{car.Make}\t{car.Model}\t{car.Year}\t{car.Price}\n");
        }
        Clipboard.SetText(text.ToString());
        Status.Text = $"Copied {Cars.FilteredItemCount:N0} rows to the clipboard";
    }
}
