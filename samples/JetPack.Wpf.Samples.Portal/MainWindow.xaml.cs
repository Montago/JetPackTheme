using System.Windows.Controls;
using System.Windows.Input;
using JetPack.Wpf.Controls;

namespace JetPack.Wpf.Samples.Portal;

public partial class MainWindow : JetPackWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public IEnumerable<string> PageNames => Pages.Items.Cast<TabItem>().Select(t => (string)t.Header);

    public void ShowPage(string name) =>
        Pages.SelectedItem = Pages.Items.Cast<TabItem>().First(t => (string)t.Header == name);

    private void OnGoToPage(object sender, ExecutedRoutedEventArgs e) => ShowPage((string)e.Parameter);
}
