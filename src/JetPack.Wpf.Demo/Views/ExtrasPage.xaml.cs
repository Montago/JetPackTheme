using System.Windows;
using System.Windows.Controls;

namespace JetPack.Wpf.Demo.Views;

public partial class ExtrasPage : UserControl
{
    public ExtrasPage()
    {
        InitializeComponent();
        HamOverview.Tag = new[] { "Oversigt", "Det valgte punkt får accentgradient og samme lille notch som fanerne, her pegende ind mod indholdet." };
        HamOrders.Tag = new[] { "Ordrer", "14 åbne ordrer, 3 forfalder i denne uge." };
        HamPeople.Tag = new[] { "Medarbejdere", "23 medarbejdere fordelt på 5 afdelinger." };
        HamSettings.Tag = new[] { "Indstillinger", "Sprog, accentfarve og synkronisering." };
    }

    private void OnOpenFilters(object sender, RoutedEventArgs e) => Filters.IsOpen = true;
}
