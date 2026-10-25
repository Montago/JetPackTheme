using System.Windows.Controls;

namespace JetPack.Wpf.Demo.Views;

public partial class SdkPage : UserControl
{
    public SdkPage()
    {
        InitializeComponent();
        Grid.ItemsSource = DemoData.Timesheets;
        Grid.SelectedIndex = 1;
        List.ItemsSource = DemoData.Timesheets;

        var today = DateTime.Today;
        Calendar.DisplayDate = today;
        Calendar.SelectedDate = today.AddDays(4);
        Picked.SelectedDate = today;
        BadDate.SelectedDate = today.AddYears(-3);

        Suggest.ItemsSource = DemoData.SuggestCompanies;
    }
}
