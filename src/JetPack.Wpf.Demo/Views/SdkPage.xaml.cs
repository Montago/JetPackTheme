using System.Windows.Controls;

namespace JetPack.Wpf.Demo.Views;

public partial class SdkPage : UserControl
{
    public static readonly Timesheet[] Timesheets =
    {
        new("Charles Hoyle", "Consulting", 37.5, 43, "Approved"),
        new("Daniel Tiedemann", "Development", 41.0, 43, "Pending"),
        new("David Ritter", "Development", 32.0, 43, "Draft"),
        new("Ida Kragh", "Finance", 37.0, 43, "Approved"),
        new("Lindsey Ballard", "Sales", 18.5, 43, "Rejected"),
        new("Mads Lund", "Support", 39.5, 43, "Approved"),
        new("Vivian Smail", "Sales", 36.0, 43, "Approved"),
    };

    public SdkPage()
    {
        InitializeComponent();
        Grid.ItemsSource = Timesheets;
        Grid.SelectedIndex = 1;
        List.ItemsSource = Timesheets;

        var today = DateTime.Today;
        Calendar.DisplayDate = today;
        Calendar.SelectedDate = today.AddDays(4);
        Picked.SelectedDate = today;
        BadDate.SelectedDate = today.AddYears(-3);

        Suggest.ItemsSource = new[]
        {
            "Contoso Corporation", "Fabrikam", "Fabrikam Residences", "Southridge Video", "Soar Airlines", "Margie's Travel", "Northwind Traders", "Fourth Coffee",
        };
    }
}
