using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using JetPack.Wpf.Controls;
using JetPack.Wpf.Theming;

namespace JetPack.Wpf.Demo;

public partial class MainWindow : JetPackWindow
{
    public MainWindow()
    {
        InitializeComponent();
        FocusSwitch.Checked += (_, _) => ThemeManager.ShowFocusVisuals = true;
        FocusSwitch.Unchecked += (_, _) => ThemeManager.ShowFocusVisuals = false;
        VignetteSwitch.Checked += (_, _) => ShowVignette = true;
        VignetteSwitch.Unchecked += (_, _) => ShowVignette = false;
        DependencyPropertyDescriptor.FromProperty(ColorPicker.SelectedColorProperty, typeof(ColorPicker))
            .AddValueChanged(CustomAccent, (_, _) => ThemeManager.ChangeAccent(CustomAccent.SelectedColor));
    }

    public void OpenSettings() => SettingsFlyout.IsOpen = true;

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

    private void OnSizeRadio(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            ThemeManager.ControlFontSize = double.Parse((string)((FrameworkElement)sender).Tag);
        }
    }
}
