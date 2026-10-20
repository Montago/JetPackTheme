using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace JetPack.Wpf.Samples.Admin;

public partial class App : Application
{
    public App()
    {
        if (WindowSnapshot.Folder != null)
        {
            WindowSnapshot.UseEnglish();
        }
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Bindings format dates and numbers with the user's culture instead of en-US.
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        var window = new MainWindow();
        MainWindow = window;
        if (WindowSnapshot.Folder == null)
        {
            window.Show();
            return;
        }

        // --snapshot <folder>: one picture per page, plus the settings flyout and a dialog.
        WindowSnapshot.ShowOffScreen(window);
        foreach (var page in window.PageNames)
        {
            window.ShowPage(page);
            await WindowSnapshot.SaveAsync(window, "admin-" + page.ToLowerInvariant());
        }
        window.ShowPage("Employees");
        window.Settings.IsOpen = true;
        await WindowSnapshot.SaveAsync(window, "admin-settings");
        window.Settings.IsOpen = false;
        _ = window.EmployeesPage.ConfirmDeleteAsync();
        await WindowSnapshot.SaveAsync(window, "admin-dialog");
        Shutdown();
    }
}
