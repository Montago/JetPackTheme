using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace JetPack.Wpf.Samples.Portal.Views;

public partial class ProductsView : UserControl
{
    private readonly ICollectionView view;

    public ProductsView()
    {
        InitializeComponent();
        view = new CollectionViewSource { Source = SampleData.Products }.View;
        view.Filter = Matches;
        List.ItemsSource = view;
        List.SelectionChanged += (_, _) => ShowStockWarning();
        List.SelectedIndex = 1;
    }

    private bool Matches(object item)
    {
        var product = (Product)item;
        var category = Category.SelectedIndex > 0 ? (string)Category.SelectedItem : null;
        return product.Name.Contains(Search.Text, StringComparison.CurrentCultureIgnoreCase)
            && (category == null || product.Category == category);
    }

    private void OnFilterChanged(object sender, RoutedEventArgs e) => view?.Refresh();

    private void ShowStockWarning() => LowStock.IsOpen = List.SelectedItem is Product { IsLow: true };
}
