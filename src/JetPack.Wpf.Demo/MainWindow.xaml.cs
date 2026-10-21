using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using JetPack.Wpf.Controls;
using JetPack.Wpf.Helpers;
using JetPack.Wpf.Theming;

namespace JetPack.Wpf.Demo;

public partial class MainWindow : JetPackWindow
{
    private readonly object stripBrand;

    public MainWindow()
    {
        InitializeComponent();
        stripBrand = Pages.Tag;
        FooterVersion.Content = $"JetPack.Wpf {typeof(JetPackWindow).Assembly.GetName().Version?.ToString(3)}";
        FocusSwitch.Checked += (_, _) => ThemeManager.ShowFocusVisuals = true;
        FocusSwitch.Unchecked += (_, _) => ThemeManager.ShowFocusVisuals = false;
        VignetteSwitch.Checked += (_, _) => ShowVignette = true;
        VignetteSwitch.Unchecked += (_, _) => ShowVignette = false;
        DependencyPropertyDescriptor.FromProperty(ColorPicker.SelectedColorProperty, typeof(ColorPicker))
            .AddValueChanged(CustomAccent, (_, _) => ThemeManager.ChangeAccent(CustomAccent.SelectedColor));
    }

    public void OpenSettings() => SettingsFlyout.IsOpen = true;

    /// <summary>
    /// Opens the demo flyout at the given edge: settings, recent items, an update banner or a
    /// notification that closes after <paramref name="notificationMs"/> milliseconds (zero keeps it open).
    /// </summary>
    public void ShowFlyout(FlyoutPosition position, long notificationMs)
    {
        SavedFlyout.IsAutoCloseEnabled = notificationMs > 0;
        SavedFlyout.AutoCloseInterval = notificationMs;

        var flyout = position switch
        {
            FlyoutPosition.Left => RecentFlyout,
            FlyoutPosition.Top => UpdateFlyout,
            FlyoutPosition.Bottom => SavedFlyout,
            _ => SettingsFlyout,
        };
        flyout.IsOpen = true;
    }

    private void OnCloseUpdate(object sender, RoutedEventArgs e) => UpdateFlyout.IsOpen = false;

    /// <summary>
    /// Switches the page tabs between the dark tab strip and the full-width grey navigation bar
    /// that runs edge to edge under the title bar.
    /// </summary>
    public void UseNavigationBar(bool bar)
    {
        // Detach the brand from the old template before the new one picks up Tag.
        Pages.Tag = null;
        Pages.Style = (Style)FindResource(bar ? "JetPack.Styles.TabControl.Navigation" : "JetPack.Styles.TabControl.Strip");
        Pages.Tag = bar ? FindResource("Demo.Brand.Bar") : stripBrand;
        PagesHost.Margin = bar ? new Thickness(0) : new Thickness(24, 16, 24, 0);
        foreach (TabItem item in Pages.Items)
        {
            ((FrameworkElement)item.Content).Margin = bar ? new Thickness(24, 0, 24, 0) : new Thickness(0);
        }
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e) => SettingsFlyout.IsOpen = !SettingsFlyout.IsOpen;

    private void OnAccentClick(object sender, RoutedEventArgs e) => ApplyAccent((string)((FrameworkElement)sender).Tag);

    private void OnAccentRadio(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            ApplyAccent((string)((FrameworkElement)sender).Tag);
        }
    }

    private static void ApplyAccent(string name) => ThemeManager.ChangeAccent(Enum.Parse<JetPackAccent>(name));

    private void OnNavigationRadio(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            UseNavigationBar((string)((FrameworkElement)sender).Tag == "Bar");
        }
    }

    private void OnNotchRadio(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            TabControlHelper.SetSelectionAnimation(Pages, Enum.Parse<TabSelectionAnimation>((string)((FrameworkElement)sender).Tag));
        }
    }

    private void OnTransitionRadio(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            TabControlHelper.SetContentTransition(Pages, Enum.Parse<TabContentTransition>((string)((FrameworkElement)sender).Tag));
        }
    }

    private void OnSizeRadio(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            ThemeManager.ControlFontSize = double.Parse((string)((FrameworkElement)sender).Tag);
        }
    }
}
