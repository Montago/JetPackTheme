// How the Palette tab groups the theme's brushes and colours. Plain C# without WPF types, so the web
// demo (src/JetPack.Web.Demo) links this file and shows the same groups in the same order.
namespace JetPack.Wpf.Demo;

public static class PaletteGroups
{
    // The first part of a key after "JetPack.Brushes." / "JetPack.Colors." picks its group; the
    // groups follow the sections of Brushes.xaml, with the accent first.
    private static readonly (string Title, string[] Parts)[] Groups =
    [
        ("Accent", ["Accent", "Focus"]),
        ("Window and text", ["Window", "Text", "Header"]),
        ("Light surfaces", ["Control", "Static", "Disabled"]),
        ("Inputs", ["Input", "Dark"]),
        ("Validation", ["Error"]),
        ("Tracks and toggles", ["Track", "Slider", "CheckBox", "RadioButton", "Toggle"]),
        ("Lists and data grid", ["Selection", "ScrollBar", "DataGrid"]),
        ("Panels and tabs", ["Panel", "Strip", "Navigation", "Tab"]),
        ("Menus, dialogs and overlays", ["Menu", "Dialog", "Overlay", "Close"]),
        ("Coloured buttons", ["Button"]),
        ("States", ["Info", "Success", "Warning"]),
        ("Chart", ["Chart"]),
    ];

    /// <summary>True for the keys the Palette tab shows: every JetPack.Brushes.* and JetPack.Colors.* resource.</summary>
    public static bool IsPaletteKey(string key) => key.StartsWith("JetPack.Brushes.", StringComparison.Ordinal) || key.StartsWith("JetPack.Colors.", StringComparison.Ordinal);

    /// <summary>The key without "JetPack.Brushes." / "JetPack.Colors.": Accent.Selection.</summary>
    public static string Name(string key) => key[(key.IndexOf('.', "JetPack.".Length) + 1)..];

    /// <summary>The bold line of a swatch: the name, marked when the resource is a Color rather than a brush.</summary>
    public static string Label(string key) => key.StartsWith("JetPack.Colors.", StringComparison.Ordinal) ? Name(key) + " (Color)" : Name(key);

    /// <summary>
    /// Sorts the keys into the groups: by first part as the group lists them, then alphabetically (a brush
    /// before the Color of the same name). Keys no group claims end up under "Other", so new resources
    /// always show up.
    /// </summary>
    public static IEnumerable<(string Title, string[] Keys)> Sort(IEnumerable<string> keys)
    {
        var all = keys.Where(IsPaletteKey).Distinct().ToList();
        var rest = new HashSet<string>(all);
        foreach (var (title, parts) in Groups)
        {
            var members = all.Where(k => parts.Contains(Name(k).Split('.')[0]))
                .OrderBy(k => Array.IndexOf(parts, Name(k).Split('.')[0]))
                .ThenBy(Name, StringComparer.Ordinal).ThenBy(k => k, StringComparer.Ordinal).ToArray();
            rest.ExceptWith(members);
            if (members.Length > 0)
                yield return (title, members);
        }
        if (rest.Count > 0)
            yield return ("Other", rest.OrderBy(Name, StringComparer.Ordinal).ToArray());
    }
}
