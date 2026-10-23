using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
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

/// <summary>Shape of the outer corners of a <see cref="JetPackWindow"/>.</summary>
public enum WindowCornerStyle
{
    /// <summary>Square corners, as in the original Silverlight theme.</summary>
    Square,

    /// <summary>
    /// Rounded corners drawn by the Windows 11 compositor. On Windows 10 the
    /// corners stay square.
    /// </summary>
    Rounded,
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
    private Color? surfaceColor;

    static JetPackWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(JetPackWindow), new FrameworkPropertyMetadata(typeof(JetPackWindow)));
        ResizeModeProperty.OverrideMetadata(typeof(JetPackWindow), new FrameworkPropertyMetadata(ResizeMode.CanResize, (d, _) => ((JetPackWindow)d).UpdateResizeBorder()));
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

    private void UpdateResizeBorder()
    {
        // Only CanResize resizes by the edges; CanResizeWithGrip resizes by the grip alone
        // (its WindowChrome.ResizeGripDirection works without a resize border).
        if (WindowChrome.GetWindowChrome(this) is { } chrome)
        {
            chrome.ResizeBorderThickness = ResizeMode == ResizeMode.CanResize ? new Thickness(6) : new Thickness(0);
        }
    }

    public static readonly DependencyProperty TitleBarModeProperty = DependencyProperty.Register(
        nameof(TitleBarMode), typeof(TitleBarMode), typeof(JetPackWindow), new PropertyMetadata(TitleBarMode.Dark));

    public TitleBarMode TitleBarMode
    {
        get => (TitleBarMode)GetValue(TitleBarModeProperty);
        set => SetValue(TitleBarModeProperty, value);
    }

    public static readonly DependencyProperty CornerStyleProperty = DependencyProperty.Register(
        nameof(CornerStyle), typeof(WindowCornerStyle), typeof(JetPackWindow),
        new PropertyMetadata(WindowCornerStyle.Square, (d, _) => ((JetPackWindow)d).ApplyCornerStyle()));

    /// <summary>Square (default) or rounded outer corners. Rounded needs Windows 11.</summary>
    public WindowCornerStyle CornerStyle
    {
        get => (WindowCornerStyle)GetValue(CornerStyleProperty);
        set => SetValue(CornerStyleProperty, value);
    }

    private static readonly DependencyPropertyKey HasSystemRoundedCornersPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasSystemRoundedCorners), typeof(bool), typeof(JetPackWindow), new PropertyMetadata(false));

    public static readonly DependencyProperty HasSystemRoundedCornersProperty = HasSystemRoundedCornersPropertyKey.DependencyProperty;

    /// <summary>
    /// True when Windows rounds the corners and draws the 1px frame itself, so
    /// the template leaves its own border out.
    /// </summary>
    public bool HasSystemRoundedCorners => (bool)GetValue(HasSystemRoundedCornersProperty);

    public static readonly DependencyProperty EnableMicaProperty = DependencyProperty.Register(
        nameof(EnableMica), typeof(bool), typeof(JetPackWindow),
        new PropertyMetadata(false, (d, _) => ((JetPackWindow)d).ApplyBackdrop()));

    /// <summary>
    /// Lets the Windows 11 Mica backdrop show through the JetPack ground (see
    /// <see cref="GroundOpacity"/>). Needs Windows 11 22H2; elsewhere the window stays opaque.
    /// </summary>
    public bool EnableMica
    {
        get => (bool)GetValue(EnableMicaProperty);
        set => SetValue(EnableMicaProperty, value);
    }

    public static readonly DependencyProperty GroundOpacityProperty = DependencyProperty.Register(
        nameof(GroundOpacity), typeof(double), typeof(JetPackWindow), new PropertyMetadata(0.5));

    /// <summary>
    /// How much the ground, texture and title bar cover the Mica backdrop while
    /// <see cref="IsMicaActive"/> is true: 0 shows Mica alone, 1 hides it.
    /// </summary>
    public double GroundOpacity
    {
        get => (double)GetValue(GroundOpacityProperty);
        set => SetValue(GroundOpacityProperty, value);
    }

    private static readonly DependencyPropertyKey IsMicaActivePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsMicaActive), typeof(bool), typeof(JetPackWindow), new PropertyMetadata(false));

    public static readonly DependencyProperty IsMicaActiveProperty = IsMicaActivePropertyKey.DependencyProperty;

    /// <summary>True while Windows draws Mica behind the window, so the template lets it through.</summary>
    public bool IsMicaActive => (bool)GetValue(IsMicaActiveProperty);

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

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyCornerStyle();
        ApplyBackdrop();
    }

    private void ApplyBackdrop()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        // Dark Mica suits the grey ground. The backdrop type fails before Windows 11 22H2.
        var active = false;
        if (EnableMica)
        {
            NativeMethods.SetDwmAttribute(hwnd, NativeMethods.DwmwaUseImmersiveDarkMode, 1);
            active = NativeMethods.SetDwmAttribute(hwnd, NativeMethods.DwmwaSystemBackdropType, NativeMethods.DwmsbtMainWindow);
        }
        else if (IsMicaActive)
        {
            NativeMethods.SetDwmAttribute(hwnd, NativeMethods.DwmwaSystemBackdropType, NativeMethods.DwmsbtNone);
            NativeMethods.SetDwmAttribute(hwnd, NativeMethods.DwmwaUseImmersiveDarkMode, 0);
        }

        // DWM only draws the backdrop where the frame reaches, so extend it over the whole window;
        // WindowChrome then also makes the WPF surface transparent, which turns ClearType off.
        // It leaves the surface transparent when the frame shrinks again, so restore it here.
        var target = HwndSource.FromHwnd(hwnd)?.CompositionTarget;
        if (active && !IsMicaActive)
        {
            surfaceColor = target?.BackgroundColor;
        }
        if (WindowChrome.GetWindowChrome(this) is { } chrome)
        {
            chrome.GlassFrameThickness = active ? new Thickness(-1) : new Thickness(0);
        }
        if (!active && surfaceColor is { } color && target != null)
        {
            target.BackgroundColor = color;
            surfaceColor = null;
        }

        // Once the surface has been transparent WPF keeps rendering text in grey scale; the ground
        // is opaque again, so tell it ClearType is safe.
        if (root != null)
        {
            if (active)
            {
                root.ClearValue(RenderOptions.ClearTypeHintProperty);
            }
            else if (IsMicaActive)
            {
                RenderOptions.SetClearTypeHint(root, ClearTypeHint.Enabled);
            }
        }
        SetValue(IsMicaActivePropertyKey, active);
    }

    private void ApplyCornerStyle()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var rounded = CornerStyle == WindowCornerStyle.Rounded;
        var applied = NativeMethods.SetDwmAttribute(hwnd, NativeMethods.DwmwaWindowCornerPreference,
            rounded ? NativeMethods.DwmwcpRound : NativeMethods.DwmwcpDoNotRound);

        // Rounded: Windows draws the frame, in the JetPack border colour. Square: no system
        // frame, so only the template's 1px border shows.
        var border = NativeMethods.DwmwaColorNone;
        if (rounded && (TryFindResource("JetPack.Brushes.Window.Border") as SolidColorBrush ?? BorderBrush as SolidColorBrush) is { } brush)
        {
            var c = brush.Color;
            border = c.R | (c.G << 8) | (c.B << 16);
        }
        NativeMethods.SetDwmAttribute(hwnd, NativeMethods.DwmwaBorderColor, border);

        SetValue(HasSystemRoundedCornersPropertyKey, rounded && applied);
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
