using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace JetPack.Wpf.Demo.Views;

/// <summary>One sample per control, rendered by --snapshot for the documentation.</summary>
public partial class GalleryPage : UserControl
{
    public GalleryPage()
    {
        InitializeComponent();
        Grid.ItemsSource = SdkPage.Timesheets;
        Grid.SelectedIndex = 1;
        List.ItemsSource = SdkPage.Timesheets;

        // Each grid gets its own array: grids on the same collection share its view, filters included.
        CarGrid.ItemsSource = CarData.Cars.Where(c => c.Make is "Volvo" or "Toyota" or "Kia").Take(60).ToArray();
        CarGrid.Items.SortDescriptions.Add(new SortDescription(nameof(Car.Price), ListSortDirection.Descending));
        CarGrid.SelectedIndex = 1;
        FilterGrid.ItemsSource = CarData.Cars.Take(80).ToArray();
        PagedGrid.ItemsSource = CarData.Cars.Skip(100).Take(95).ToArray();
        PagedGrid.PageIndex = 1;
        GroupGrid.ItemsSource = CarData.Cars.Where(c => c.Make is "Skoda" or "Tesla").Take(14).OrderBy(c => c.Make).ToArray();

        // Fixed dates keep the pictures stable from run to run.
        var day = new DateTime(2026, 10, 16);
        Calendar.DisplayDate = day;
        Calendar.SelectedDate = day.AddDays(4);
        OpenDate.SelectedDate = day;
        Time.SelectedTime = new TimeSpan(8, 30, 0);

        Suggest.ItemsSource = new[]
        {
            "Contoso Corporation", "Fabrikam", "Fabrikam Residences", "Southridge Video", "Soar Airlines", "Margie's Travel", "Northwind Traders", "Fourth Coffee",
        };
    }

    /// <summary>The samples; each one's <c>Tag</c> is its file name.</summary>
    public IEnumerable<ContentControl> Samples => SamplesHost.Children.OfType<ContentControl>();
}
