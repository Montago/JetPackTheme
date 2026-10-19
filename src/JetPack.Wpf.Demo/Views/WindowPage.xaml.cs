using System.Windows;
using System.Windows.Controls;
using JetPack.Wpf.Controls;

namespace JetPack.Wpf.Demo.Views;

public partial class WindowPage : UserControl
{
    public WindowPage()
    {
        InitializeComponent();
        Cal.DisplayDate = DateTime.Today;
        Cal.SelectedDate = DateTime.Today.AddDays(4);
    }

    private JetPackWindow? Host => Window.GetWindow(this) as JetPackWindow;

    private void OnTitleMode(object sender, RoutedEventArgs e)
    {
        if (Host is { } window)
        {
            window.TitleBarMode = Enum.Parse<TitleBarMode>((string)((FrameworkElement)sender).Tag);
        }
    }

    private void OnCornerStyle(object sender, RoutedEventArgs e)
    {
        if (Host is { } window)
        {
            window.CornerStyle = Enum.Parse<WindowCornerStyle>((string)((FrameworkElement)sender).Tag);
        }
    }

    private async void OnShowDialog(object sender, RoutedEventArgs e)
    {
        if (Host is { } window)
        {
            await window.ShowMessageAsync("Timesheet sent", "Your timesheet for week 43 has been sent to Ida Kragh for approval.");
        }
    }

    private async void OnShowDelete(object sender, RoutedEventArgs e)
    {
        if (Host is not { } window)
        {
            return;
        }
        var dialog = new JetPackDialog
        {
            Title = "Delete company?",
            Content = "Contoso Corporation and 3 linked placements will be deleted permanently. Timesheets that have already been billed stay in the archive.",
            AffirmativeText = "Delete",
            NegativeText = "Cancel",
            AffirmativeButtonStyle = (Style)FindResource("JetPack.Styles.Button.Orange"),
        };
        var result = await window.ShowDialogAsync(dialog);
        if (result == MessageDialogResult.Affirmative)
        {
            await window.ShowMessageAsync("Deleted", "Contoso Corporation has been deleted.");
        }
    }
}
