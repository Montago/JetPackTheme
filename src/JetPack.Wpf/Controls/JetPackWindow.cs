using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shell;

namespace JetPack.Wpf.Controls;

/// <summary>How the title bar of a <see cref="JetPackWindow"/> is painted.</summary>
public enum TitleBarMode
{
    /// <summary>Dark grey gradient (the data grid header), white text.</summary>
    Dark,

    /// <summary>Light grey gradient (the navigation bar), dark embossed text.</summary>
    Light,

    /// <summary>No bar: the texture runs all the way to the top.</summary>
    Texture,
}

/// <summary>
/// Window with the JetPack frame: tiled grey texture, vignette, custom title
/// bar with window commands, a flyout layer and an overlay for dialogs.
/// </summary>
[TemplatePart(Name = PartDialogHost, Type = typeof(Grid))]
public class JetPackWindow : Window
{
    private const string PartDialogHost = "PART_DialogHost";
    private const string PartRoot = "PART_Root";

    private Grid? dialogHost;
    private FrameworkElement? root;

    static JetPackWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(JetPackWindow), new FrameworkPropertyMetadata(typeof(JetPackWindow)));
    }

    public JetPackWindow()
    {
        WindowChrome.SetWindowChrome(this, new WindowChrome
        {
            CaptionHeight = 30,
            ResizeBorderThickness = new Thickness(6),
            GlassFrameThickness = new Thickness(0),
            CornerRadius = new CornerRadius(0),
            UseAeroCaptionButtons = false,
        });

        CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, (_, _) => SystemCommands.MinimizeWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, (_, _) => SystemCommands.MaximizeWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, (_, _) => SystemCommands.RestoreWindow(this)));
        CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, (_, _) => SystemCommands.CloseWindow(this)));
    }

    public static readonly DependencyProperty TitleBarModeProperty = DependencyProperty.Register(
        nameof(TitleBarMode), typeof(TitleBarMode), typeof(JetPackWindow), new PropertyMetadata(TitleBarMode.Dark));

    public TitleBarMode TitleBarMode
    {
        get => (TitleBarMode)GetValue(TitleBarModeProperty);
        set => SetValue(TitleBarModeProperty, value);
    }

    public static readonly DependencyProperty TitleBarHeightProperty = DependencyProperty.Register(
        nameof(TitleBarHeight), typeof(double), typeof(JetPackWindow), new PropertyMetadata(30d, OnTitleBarHeightChanged));

    public double TitleBarHeight
    {
        get => (double)GetValue(TitleBarHeightProperty);
        set => SetValue(TitleBarHeightProperty, value);
    }

    private static void OnTitleBarHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (WindowChrome.GetWindowChrome((Window)d) is { } chrome)
        {
            chrome.CaptionHeight = (double)e.NewValue;
        }
    }

    public static readonly DependencyProperty ShowIconProperty = DependencyProperty.Register(
        nameof(ShowIcon), typeof(bool), typeof(JetPackWindow), new PropertyMetadata(true));

    public bool ShowIcon
    {
        get => (bool)GetValue(ShowIconProperty);
        set => SetValue(ShowIconProperty, value);
    }

    public static readonly DependencyProperty ShowVignetteProperty = DependencyProperty.Register(
        nameof(ShowVignette), typeof(bool), typeof(JetPackWindow), new PropertyMetadata(true));

    /// <summary>The darkened edges of the original theme. On by default.</summary>
    public bool ShowVignette
    {
        get => (bool)GetValue(ShowVignetteProperty);
        set => SetValue(ShowVignetteProperty, value);
    }

    public static readonly DependencyProperty ShowMinButtonProperty = DependencyProperty.Register(
        nameof(ShowMinButton), typeof(bool), typeof(JetPackWindow), new PropertyMetadata(true));

    public bool ShowMinButton
    {
        get => (bool)GetValue(ShowMinButtonProperty);
        set => SetValue(ShowMinButtonProperty, value);
    }

    public static readonly DependencyProperty ShowMaxRestoreButtonProperty = DependencyProperty.Register(
        nameof(ShowMaxRestoreButton), typeof(bool), typeof(JetPackWindow), new PropertyMetadata(true));

    public bool ShowMaxRestoreButton
    {
        get => (bool)GetValue(ShowMaxRestoreButtonProperty);
        set => SetValue(ShowMaxRestoreButtonProperty, value);
    }

    public static readonly DependencyProperty LeftWindowCommandsProperty = DependencyProperty.Register(
        nameof(LeftWindowCommands), typeof(object), typeof(JetPackWindow), new PropertyMetadata(null));

    /// <summary>Content shown in the title bar after the icon (buttons get the window-command look).</summary>
    public object? LeftWindowCommands
    {
        get => GetValue(LeftWindowCommandsProperty);
        set => SetValue(LeftWindowCommandsProperty, value);
    }

    public static readonly DependencyProperty RightWindowCommandsProperty = DependencyProperty.Register(
        nameof(RightWindowCommands), typeof(object), typeof(JetPackWindow), new PropertyMetadata(null));

    /// <summary>Content shown in the title bar before the minimise button.</summary>
    public object? RightWindowCommands
    {
        get => GetValue(RightWindowCommandsProperty);
        set => SetValue(RightWindowCommandsProperty, value);
    }

    public static readonly DependencyProperty FlyoutsProperty = DependencyProperty.Register(
        nameof(Flyouts), typeof(object), typeof(JetPackWindow), new PropertyMetadata(null));

    /// <summary>Layer above the content for <see cref="Flyout"/> panels (usually a Grid).</summary>
    public object? Flyouts
    {
        get => GetValue(FlyoutsProperty);
        set => SetValue(FlyoutsProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        dialogHost = GetTemplateChild(PartDialogHost) as Grid;
        root = GetTemplateChild(PartRoot) as FrameworkElement;
        UpdateMaximizedMargin();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        UpdateMaximizedMargin();
    }

    private void UpdateMaximizedMargin()
    {
        if (root == null)
        {
            return;
        }

        // A maximised chromeless window hangs over the screen edge by the resize border.
        var b = SystemParameters.WindowResizeBorderThickness;
        root.Margin = WindowState == WindowState.Maximized
            ? new Thickness(b.Left + 4, b.Top + 4, b.Right + 4, b.Bottom + 4)
            : new Thickness(0);
    }

    /// <summary>Shows a dialog over the window content and waits for it to close.</summary>
    public async Task<MessageDialogResult> ShowDialogAsync(JetPackDialog dialog)
    {
        if (dialogHost == null)
        {
            ApplyTemplate();
            dialogHost = GetTemplateChild(PartDialogHost) as Grid
                ?? throw new InvalidOperationException("The window template has no PART_DialogHost.");
        }

        var focused = Keyboard.FocusedElement;
        dialogHost.Children.Add(dialog);
        dialogHost.Visibility = Visibility.Visible;
        try
        {
            return await dialog.WaitForResultAsync();
        }
        finally
        {
            dialogHost.Children.Remove(dialog);
            if (dialogHost.Children.Count == 0)
            {
                dialogHost.Visibility = Visibility.Collapsed;
            }
            focused?.Focus();
        }
    }

    /// <summary>Shows a message with one or two buttons.</summary>
    public Task<MessageDialogResult> ShowMessageAsync(string title, string message, string affirmativeText = "OK", string? negativeText = null)
    {
        var dialog = new JetPackDialog
        {
            Title = title,
            Content = message,
            AffirmativeText = affirmativeText,
            NegativeText = negativeText,
        };
        return ShowDialogAsync(dialog);
    }
}
