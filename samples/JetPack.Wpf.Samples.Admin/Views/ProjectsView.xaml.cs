using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace JetPack.Wpf.Samples.Admin.Views;

public partial class ProjectsView : UserControl
{
    public ProjectsView()
    {
        InitializeComponent();
        Loaded += (_, _) => Dispatcher.BeginInvoke(SelectFirstProject, DispatcherPriority.Loaded);
    }

    // TreeView has no SelectedIndex; select the first project once its container exists.
    private void SelectFirstProject()
    {
        if (Tree.SelectedItem != null
            || Tree.ItemContainerGenerator.ContainerFromIndex(0) is not TreeViewItem customer)
        {
            return;
        }
        customer.UpdateLayout();
        if (customer.ItemContainerGenerator.ContainerFromIndex(0) is TreeViewItem project)
        {
            project.IsSelected = true;
        }
    }
}
