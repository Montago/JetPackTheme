using System.Windows.Controls;

namespace JetPack.Wpf.Demo.Views;

public partial class SdkPage : UserControl
{
    public static readonly Timesheet[] Timesheets =
    {
        new("Charles Hoyle", "Rådgivning", 37.5, 43, "Godkendt"),
        new("Daniel Tiedemann", "Udvikling", 41.0, 43, "Afventer"),
        new("David Ritter", "Udvikling", 32.0, 43, "Kladde"),
        new("Ida Kragh", "Økonomi", 37.0, 43, "Godkendt"),
        new("Lindsey Ballard", "Salg", 18.5, 43, "Afvist"),
        new("Mads Lund", "Support", 39.5, 43, "Godkendt"),
        new("Vivian Smail", "Salg", 36.0, 43, "Godkendt"),
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
            "Contoso A/S", "Fabrikam", "Faber & Søn ApS", "Southridge Video", "Soar Airlines", "Microsoft Danmark", "Northwind Traders", "Fourth Coffee",
        };
    }
}
