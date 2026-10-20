using System.Windows;
using System.Windows.Input;
using JetPack.Wpf.Controls;
using JetPack.Wpf.Samples.Admin.Views;
using JetPack.Wpf.Theming;

namespace JetPack.Wpf.Samples.Admin;

public partial class MainWindow : JetPackWindow
{
    public MainWindow()
    {
        InitializeComponent();
        Version.Content = $"JetPack.Wpf {typeof(JetPackWindow).Assembly.GetName().Version?.ToString(3)}";
        FocusSwitch.Checked += (_, _) => ThemeManager.ShowFocusVisuals = true;
        FocusSwitch.Unchecked += (_, _) => ThemeManager.ShowFocusVisuals = false;
    }

    public IEnumerable<string> PageNames => Navigation.Items.Cast<HamburgerMenuItem>().Select(i => (string)i.Content);

    public EmployeesView EmployeesPage => EmployeesView;

    public void ShowPage(string name) =>
        Navigation.SelectedItem = Navigation.Items.Cast<HamburgerMenuItem>().First(i => (string)i.Content == name);

    private void OnGoToPage(object sender, ExecutedRoutedEventArgs e) => ShowPage((string)e.Parameter);

    private void OnSettingsClick(object sender, RoutedEventArgs e) => Settings.IsOpen = !Settings.IsOpen;

    private void OnAccentChecked(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            ThemeManager.ChangeAccent(Enum.Parse<JetPackAccent>((string)((FrameworkElement)sender).Tag));
        }
    }
}
