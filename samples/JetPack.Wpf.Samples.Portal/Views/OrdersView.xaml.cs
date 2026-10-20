using System.Windows.Controls;

namespace JetPack.Wpf.Samples.Portal.Views;

public partial class OrdersView : UserControl
{
    public OrdersView()
    {
        InitializeComponent();
        From.SelectedDate = new DateTime(2026, 10, 1);
        To.SelectedDate = new DateTime(2026, 10, 16);
    }
}
