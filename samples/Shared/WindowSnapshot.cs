using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace JetPack.Wpf.Samples;

/// <summary>
/// Support for <c>--snapshot &lt;folder&gt;</c>: the sample apps render their pages to PNG for the
/// documentation and exit. Not needed in a real application.
/// </summary>
internal static class WindowSnapshot
{
    /// <summary>The folder after <c>--snapshot</c>, or null when the app runs normally.</summary>
    public static string? Folder { get; } = GetFolder();

    private static string? GetFolder()
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, "--snapshot");
        return index < 0 ? null : index + 1 < args.Length ? args[index + 1] : "snapshots";
    }

    /// <summary>Forces en-US so the pictures stay English. Call from the App constructor.</summary>
    public static void UseEnglish()
    {
        var english = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = english;
        Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = english;
    }

    /// <summary>Shows the window off the screen at a fixed size.</summary>
    public static void ShowOffScreen(Window window, double width = 1280, double height = 800)
    {
        window.ShowActivated = false;
        window.ShowInTaskbar = false;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -20000;
        window.Top = 0;
        window.Width = width;
        window.Height = height;
        window.Show();
    }

    /// <summary>Waits for layout and data loading to settle, then saves the window as PNG.</summary>
    public static async Task SaveAsync(Window window, string name, int delay = 400)
    {
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        await Task.Delay(delay);
        await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

        Directory.CreateDirectory(Folder!);
        var root = (FrameworkElement)VisualTreeHelper.GetChild(window, 0);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth), (int)Math.Ceiling(root.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        await using var stream = File.Create(Path.Combine(Folder!, name + ".png"));
        encoder.Save(stream);
    }
}
