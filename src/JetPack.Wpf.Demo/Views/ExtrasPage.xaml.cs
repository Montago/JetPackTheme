using System.Windows;
using System.Windows.Controls;

namespace JetPack.Wpf.Demo.Views;

public partial class ExtrasPage : UserControl
{
    public ExtrasPage()
    {
        InitializeComponent();
        HamOverview.Tag = new[] { "Overview", "The selected item gets the accent gradient and the same small notch as the tabs, here pointing at the content." };
        HamOrders.Tag = new[] { "Orders", "14 open orders, 3 due this week." };
        HamPeople.Tag = new[] { "Employees", "23 employees in 5 departments." };
        HamSettings.Tag = new[] { "Settings", "Language, accent colour and syncing." };
    }

    private void OnOpenFilters(object sender, RoutedEventArgs e) => Filters.IsOpen = true;
}
