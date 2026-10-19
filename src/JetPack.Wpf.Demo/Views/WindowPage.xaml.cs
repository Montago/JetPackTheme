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

    private async void OnShowDialog(object sender, RoutedEventArgs e)
    {
        if (Host is { } window)
        {
            await window.ShowMessageAsync("Ugeseddel sendt", "Din ugeseddel for uge 43 er sendt til godkendelse hos Ida Kragh.");
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
            Title = "Slet virksomhed?",
            Content = "Contoso A/S og 3 tilknyttede placeringer slettes permanent. Ugesedler, der allerede er faktureret, bevares i arkivet.",
            AffirmativeText = "Slet",
            NegativeText = "Annuller",
            AffirmativeButtonStyle = (Style)FindResource("JetPack.Styles.Button.Orange"),
        };
        var result = await window.ShowDialogAsync(dialog);
        if (result == MessageDialogResult.Affirmative)
        {
            await window.ShowMessageAsync("Slettet", "Contoso A/S er slettet.");
        }
    }
}
