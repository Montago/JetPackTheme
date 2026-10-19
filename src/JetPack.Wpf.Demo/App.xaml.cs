using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using JetPack.Wpf.Controls;
using JetPack.Wpf.Theming;

namespace JetPack.Wpf.Demo;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // WPF formats bindings as en-US unless told otherwise.
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

        var window = new MainWindow();
        MainWindow = window;

        var index = Array.IndexOf(e.Args, "--snapshot");
        if (index < 0)
        {
            window.Show();
            return;
        }

        // --snapshot <folder>: render every page to PNG and exit (used to check the theme).
        var folder = index + 1 < e.Args.Length ? e.Args[index + 1] : "snapshots";
        Directory.CreateDirectory(folder);
        window.ShowActivated = false;
        window.ShowInTaskbar = false;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -20000;
        window.Top = 0;
        window.Width = 1280;
        window.Height = 900;
        window.Show();

        try
        {
            var pages = window.Pages;
            for (var i = 0; i < pages.Items.Count; i++)
            {
                pages.SelectedIndex = i;
                var name = ((TabItem)pages.Items[i]).Header?.ToString()?.ToLowerInvariant() ?? i.ToString();
                await SaveAsync(window, Path.Combine(folder, $"{i + 1:00}-{name}.png"));
            }

            pages.SelectedIndex = 0;
            ThemeManager.ChangeAccent(JetPackAccent.Orange);
            await SaveAsync(window, Path.Combine(folder, "20-core-orange.png"));
            ThemeManager.ChangeAccent(JetPackAccent.Blue);
            await SaveAsync(window, Path.Combine(folder, "21-core-blue.png"));
            ThemeManager.ChangeAccent(JetPackAccent.Green);

            pages.SelectedIndex = 4;
            window.TitleBarMode = TitleBarMode.Light;
            await SaveAsync(window, Path.Combine(folder, "22-window-light.png"));
            window.TitleBarMode = TitleBarMode.Dark;

            var dialog = window.ShowMessageAsync("Slet virksomhed?",
                "Contoso A/S og 3 tilknyttede placeringer slettes permanent. Ugesedler, der allerede er faktureret, bevares i arkivet.",
                "Slet", "Annuller");
            await SaveAsync(window, Path.Combine(folder, "23-dialog.png"));

            window.OpenSettings();
            await Task.Delay(400);
            await SaveAsync(window, Path.Combine(folder, "24-flyout.png"));
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(folder, "error.txt"), ex.ToString());
        }

        Shutdown();
    }

    private static async Task SaveAsync(Window window, string file)
    {
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        await Task.Delay(250);
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

        var root = (FrameworkElement)VisualTreeHelper.GetChild(window, 0);
        var width = (int)Math.Ceiling(root.ActualWidth);
        var height = (int)Math.Ceiling(root.ActualHeight);
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        await using var stream = File.Create(file);
        encoder.Save(stream);
    }
}
