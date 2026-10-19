using System.Windows;
using System.Windows.Media;

namespace JetPack.Wpf.Theming;

/// <summary>The accents that ship with JetPack.</summary>
public enum JetPackAccent
{
    Green,
    Orange,
    Blue,
}

/// <summary>
/// Switches accent, focus visuals and control font size at runtime by adding
/// dictionaries at the end of <see cref="Application.Resources"/>, where they
/// override the defaults merged from <c>JetPack.xaml</c>.
/// </summary>
public static class ThemeManager
{
    private static ResourceDictionary? accentDictionary;
    private static ResourceDictionary? overrideDictionary;
    private static bool showFocusVisuals = true;
    private static double controlFontSize = 12;

    public static event EventHandler? AccentChanged;

    /// <summary>Name of the active accent ("Green", "Orange", "Blue" or a generated colour).</summary>
    public static string CurrentAccent { get; private set; } = nameof(JetPackAccent.Green);

    public static void ChangeAccent(JetPackAccent accent)
    {
        var dictionary = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/JetPack.Wpf;component/Styles/Accents/{accent}.xaml", UriKind.Absolute),
        };
        Apply(dictionary, accent.ToString());
    }

    /// <summary>Generates an accent from one colour and applies it.</summary>
    public static void ChangeAccent(Color color, string? name = null)
    {
        var label = name ?? color.ToString();
        Apply(AccentGenerator.Create(color, label), label);
    }

    /// <summary>
    /// Shows a 1px accent ring around keyboard-focused controls and an accent
    /// border on focused inputs. The original Silverlight theme had none.
    /// </summary>
    public static bool ShowFocusVisuals
    {
        get => showFocusVisuals;
        set
        {
            showFocusVisuals = value;
            ApplyOverrides();
        }
    }

    /// <summary>Font size used by controls. 12 by default; the Silverlight original used 11.</summary>
    public static double ControlFontSize
    {
        get => controlFontSize;
        set
        {
            controlFontSize = value;
            ApplyOverrides();
        }
    }

    private static ResourceDictionary? AppResources => Application.Current?.Resources;

    private static void Apply(ResourceDictionary dictionary, string name)
    {
        var resources = AppResources ?? throw new InvalidOperationException("ThemeManager needs an Application instance.");
        var merged = resources.MergedDictionaries;
        if (accentDictionary != null)
        {
            merged.Remove(accentDictionary);
        }
        merged.Add(dictionary);
        accentDictionary = dictionary;
        CurrentAccent = name;
        ApplyOverrides();
        AccentChanged?.Invoke(null, EventArgs.Empty);
    }

    private static void ApplyOverrides()
    {
        var resources = AppResources;
        if (resources == null)
        {
            return;
        }

        var merged = resources.MergedDictionaries;
        if (overrideDictionary != null)
        {
            merged.Remove(overrideDictionary);
        }

        var d = new ResourceDictionary();
        if (!showFocusVisuals)
        {
            d["JetPack.Brushes.Focus"] = Brushes.Transparent;
            d["JetPack.Brushes.Focus.Border"] = Brushes.Transparent;
        }
        if (Math.Abs(controlFontSize - 12) > 0.01)
        {
            d["JetPack.Font.Size.Control"] = controlFontSize;
        }

        overrideDictionary = d;
        merged.Add(d);
    }
}
