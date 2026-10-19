using System.Windows;
using System.Windows.Controls;

namespace JetPack.Wpf.Demo.Views;

public partial class CorePage : UserControl
{
    private int repeats;

    public CorePage() => InitializeComponent();

    private void OnRepeat(object sender, RoutedEventArgs e) => Repeat.Content = $"Repeat ({++repeats})";
}
