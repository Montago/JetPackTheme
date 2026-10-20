# JetPack for WPF

The JetPack theme from Microsoft's Silverlight 4 Application Themes, rebuilt as a complete controls
library for WPF on .NET 8 and later. One resource dictionary gives every standard WPF control the
JetPack look, and the library adds the controls a desktop app usually needs: `JetPackWindow`, in-window
dialogs, `ToggleSwitch`, `NumericUpDown`, `Flyout`, `HamburgerMenu`, `InfoBar`, `RangeSlider`,
`TimePicker`, `ColorPicker`, `AutoSuggestBox`, `SearchComboBox`, `BusyIndicator` and more.

![JetPack for WPF](https://raw.githubusercontent.com/Montago/JetPackTheme/master/docs/screenshots/core.png)

## Getting started

```bash
dotnet add package JetPack.Wpf
```

Merge the theme in `App.xaml`:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://application:,,,/JetPack.Wpf;component/Styles/JetPack.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

Derive your window from `JetPackWindow`:

```xml
<jp:JetPackWindow x:Class="MyApp.MainWindow"
                  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                  xmlns:jp="urn:jetpack-wpf"
                  Title="My app">
    ...
</jp:JetPackWindow>
```

```csharp
public partial class MainWindow : JetPack.Wpf.Controls.JetPackWindow { ... }
```

Switch accent at runtime:

```csharp
JetPack.Wpf.Theming.ThemeManager.ChangeAccent(JetPack.Wpf.Theming.JetPackAccent.Orange);
```

## Documentation

Every control, the theme and accents, typography and all resource keys:
**https://montago.github.io/JetPackTheme/**

Source, demo app and issues: https://github.com/Montago/JetPackTheme

## License

MIT, except the two images `Assets/Texture.png` and `Assets/Vignette.png`, which are copied unchanged
from Microsoft's Silverlight 4 Application Themes and belong to Microsoft. See `NOTICE.md` in the
package.
