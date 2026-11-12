using System.Windows;

namespace JetPack.Wpf.Theming;

/// <summary>
/// The dark version as a resource dictionary: the normal brush keys mapped onto the dark values of
/// <c>Styles/Brushes.Dark.xaml</c>. <see cref="ThemeManager.ChangeTheme"/> adds one at the end of
/// <see cref="Application.Resources"/>, so every control turns dark; the <c>JetPack.Styles.*.Dark</c>
/// styles carry one in their <c>Style.Resources</c>, so one control (and what it holds, its drop-down
/// included) turns dark. The coloured styles (Accent, Green, Orange, Blue) use keys of their own and
/// keep their colours.
/// </summary>
public class DarkResources : ResourceDictionary
{
    /// <summary>
    /// Normal key, dark key. tools/generate_web_tokens.py reads this table for the .jp-theme-dark
    /// block of the web tokens, so keep one pair per line.
    /// </summary>
    internal static readonly (string Key, string DarkKey)[] Map =
    [
        // Raised surfaces: buttons, tiles, expander headers, menus, tool bars, calendar header
        ("JetPack.Brushes.Control.Foreground", "JetPack.Brushes.Dark.Control.Foreground"),
        ("JetPack.Brushes.Control.Foreground.Disabled", "JetPack.Brushes.Dark.Control.Foreground.Disabled"),
        ("JetPack.Brushes.Control.Background", "JetPack.Brushes.Dark.Control.Background"),
        ("JetPack.Brushes.Control.Background.MouseOver", "JetPack.Brushes.Dark.Control.Background.MouseOver"),
        ("JetPack.Brushes.Control.Background.Pressed", "JetPack.Brushes.Dark.Control.Background.Pressed"),
        ("JetPack.Brushes.Control.Background.Checked", "JetPack.Brushes.Dark.Control.Background.Checked"),
        ("JetPack.Brushes.Control.Border", "JetPack.Brushes.Dark.Input.Border"),
        ("JetPack.Brushes.Control.Glyph", "JetPack.Brushes.Dark.Control.Glyph"),
        ("JetPack.Brushes.Control.Glyph.Dark", "JetPack.Brushes.Dark.Control.Glyph"),
        ("JetPack.Brushes.Header.Foreground", "JetPack.Brushes.Dark.Control.Foreground"),
        ("JetPack.Brushes.Static.Background", "JetPack.Brushes.Dark.Control.Background"),
        ("JetPack.Brushes.Static.Background.MouseOver", "JetPack.Brushes.Dark.Control.Background.MouseOver"),
        ("JetPack.Brushes.Disabled", "JetPack.Brushes.Dark.Disabled"),
        ("JetPack.Effects.TextEmboss", "JetPack.Effects.Dark.TextEmboss"),
        // Inputs and the closed ComboBox / ColorPicker: the dark field
        ("JetPack.Brushes.Input.Background", "JetPack.Brushes.Dark.Input.Background"),
        ("JetPack.Brushes.Input.Foreground", "JetPack.Brushes.Dark.Input.Foreground"),
        ("JetPack.Brushes.Input.Watermark", "JetPack.Brushes.Dark.Input.Watermark"),
        ("JetPack.Brushes.Input.ReadOnly", "JetPack.Brushes.Dark.Input.ReadOnly"),
        ("JetPack.Brushes.Input.Disabled", "JetPack.Brushes.Dark.Input.Disabled"),
        ("JetPack.Brushes.Input.InnerShadowDark", "JetPack.Brushes.Dark.Input.InnerShadowDark"),
        ("JetPack.Brushes.Input.InnerShadowLight", "JetPack.Brushes.Dark.Input.InnerShadowLight"),
        ("JetPack.Brushes.ComboBox.Background", "JetPack.Brushes.Dark.Input.Background"),
        ("JetPack.Brushes.ComboBox.Background.MouseOver", "JetPack.Brushes.Dark.Input.Background.MouseOver"),
        ("JetPack.Brushes.ComboBox.Background.Pressed", "JetPack.Brushes.Dark.Input.Background.Pressed"),
        // Lists, grids, scroll bars
        ("JetPack.Brushes.Selection.MouseOver", "JetPack.Brushes.Dark.Selection.MouseOver"),
        ("JetPack.Brushes.DataGrid.Background", "JetPack.Brushes.Dark.DataGrid.Background"),
        ("JetPack.Brushes.DataGrid.AlternatingRow", "JetPack.Brushes.Dark.DataGrid.AlternatingRow"),
        ("JetPack.Brushes.DataGrid.GridLines", "JetPack.Brushes.Dark.DataGrid.GridLines"),
        ("JetPack.Brushes.DataGrid.SearchHit", "JetPack.Brushes.Dark.DataGrid.SearchHit"),
        ("JetPack.Brushes.ScrollBar.Track", "JetPack.Brushes.Dark.ScrollBar.Track"),
        ("JetPack.Brushes.ScrollBar.Thumb", "JetPack.Brushes.Dark.ScrollBar.Thumb"),
        ("JetPack.Brushes.ScrollBar.Thumb.Vertical", "JetPack.Brushes.Dark.ScrollBar.Thumb.Vertical"),
        ("JetPack.Brushes.ScrollBar.Thumb.Highlight", "JetPack.Brushes.Dark.ScrollBar.Thumb.Highlight"),
        // Calendar: the field of the dark inputs, light text
        ("JetPack.Brushes.Calendar.Background", "JetPack.Brushes.Dark.Input.Background"),
        ("JetPack.Brushes.Calendar.Foreground", "JetPack.Brushes.Dark.Control.Foreground"),
        ("JetPack.Brushes.Calendar.DayName", "JetPack.Brushes.Dark.Text.Dim"),
        ("JetPack.Brushes.Calendar.Today", "JetPack.Brushes.Dark.Calendar.Today"),
        ("JetPack.Brushes.Calendar.MouseOver", "JetPack.Brushes.Dark.Calendar.MouseOver"),
        // Menus, dialogs, busy indicator
        ("JetPack.Brushes.Menu.Background", "JetPack.Brushes.Dark.Panel.Background"),
        ("JetPack.Brushes.Menu.Disabled", "JetPack.Brushes.Dark.Control.Foreground.Disabled"),
        ("JetPack.Brushes.Menu.Gesture", "JetPack.Brushes.Dark.Text.Dim"),
        ("JetPack.Brushes.Separator.Light", "JetPack.Brushes.Dark.Separator.Light"),
        ("JetPack.Brushes.Dialog.Background", "JetPack.Brushes.Dark.Panel.Background"),
        ("JetPack.Brushes.Dialog.Close", "JetPack.Brushes.Dark.Dialog.Close"),
        ("JetPack.Brushes.BusyIndicator.Overlay", "JetPack.Brushes.Dark.BusyIndicator.Overlay"),
    ];

    private static ResourceDictionary? values;

    public DarkResources()
    {
        values ??= new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/JetPack.Wpf;component/Styles/Brushes.Dark.xaml", UriKind.Absolute),
        };
        foreach (var (key, darkKey) in Map)
        {
            this[key] = values[darkKey];
        }
    }
}
