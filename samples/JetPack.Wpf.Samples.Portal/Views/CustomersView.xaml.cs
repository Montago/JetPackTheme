using System.Windows.Controls;
using System.Windows.Threading;

namespace JetPack.Wpf.Samples.Portal.Views;

public partial class CustomersView : UserControl
{
    public CustomersView()
    {
        InitializeComponent();
        Loaded += (_, _) => Dispatcher.BeginInvoke(SelectFirstCustomer, DispatcherPriority.Loaded);
    }

    // TreeView has no SelectedIndex; select a customer once the containers exist.
    private void SelectFirstCustomer()
    {
        if (Tree.SelectedItem != null
            || Tree.ItemContainerGenerator.ContainerFromIndex(3) is not TreeViewItem country)
        {
            return;
        }
        country.UpdateLayout();
        if (country.ItemContainerGenerator.ContainerFromIndex(0) is TreeViewItem customer)
        {
            customer.IsSelected = true;
        }
    }
}
