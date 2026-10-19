# JetPack for WPF

JetPack-temaet fra Silverlight 4 Theme Pack, genopbygget som et komplet kontrolbibliotek til WPF på .NET 8 og nyere. Det er bygget efter samme model som MahApps.Metro: én ressourceordbog giver alle standardkontroller JetPack-udseendet, og biblioteket har desuden egne kontroller (vindue, ToggleSwitch, NumericUpDown, Flyout, HamburgerMenu og flere).

![Core](docs/screenshots/core.png)

## Kom i gang

Referér projektet (eller pakken `JetPack.Wpf`) og flet ordbogen ind i `App.xaml`:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="pack://application:,,,/JetPack.Wpf;component/Styles/JetPack.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

Vinduet arver fra `JetPackWindow`:

```xml
<jp:JetPackWindow x:Class="MyApp.MainWindow"
                  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                  xmlns:jp="urn:jetpack-wpf"
                  Title="Min app" TitleBarMode="Dark">
    ...
</jp:JetPackWindow>
```

```csharp
public partial class MainWindow : JetPack.Wpf.Controls.JetPackWindow { ... }
```

## Tema og accent

```csharp
using JetPack.Wpf.Theming;

ThemeManager.ChangeAccent(JetPackAccent.Orange);          // Green (standard), Orange, Blue
ThemeManager.ChangeAccent(Color.FromRgb(0x8E, 0x44, 0xAD)); // generér et accentsæt ud fra én farve
ThemeManager.ShowFocusVisuals = false;                    // originalens usynlige fokus
ThemeManager.ControlFontSize = 11;                        // originalens skriftstørrelse (standard: 12)
```

Ressourcenøglerne følger MahApps-skemaet med præfikset `JetPack.`: `JetPack.Brushes.Accent`, `JetPack.Brushes.Accent.Selection`, `JetPack.Brushes.Control.Background`, `JetPack.Styles.Button.Accent` og så videre. Fanen *Palet* i demoen viser dem alle.

## Hvad er med

| Gruppe | Kontroller |
| --- | --- |
| Standard-WPF (implicitte stilarter) | Button, RepeatButton, ToggleButton, Hyperlink, TextBox, PasswordBox, RichTextBox, Label, CheckBox, RadioButton, ComboBox, ListBox, ListView/GridView, TreeView, ScrollBar, ScrollViewer, ProgressBar, Slider, ToolTip, GroupBox, Expander, TabControl, Calendar, DatePicker, DataGrid, GridSplitter, Separator, Menu, ContextMenu, ToolBar, StatusBar, valideringsfejl |
| Navngivne varianter | `Button.Accent/Green/Orange/Blue/Square/Flat/Link`, `TextBox.Dark`, `TextBox.MultiLine(.Dark)`, `PasswordBox.Dark`, `TabControl.Navigation` (den lyse linkbjælke med notch), `TabControl.Strip` (den mørke fanestrimmel), `Expander.Accordion`, `TextBlock.Heading1–6` |
| Egne kontroller (`jp:`) | JetPackWindow, JetPackDialog, ToggleSwitch, NumericUpDown, SplitButton, DropDownButton, ProgressRing, Badged, Tile, Flyout, HamburgerMenu, InfoBar, RangeSlider, Rating, TimePicker, ColorPicker, AutoSuggestBox, BusyIndicator, Accordion |
| Hjælpere | `TextBoxHelper.Watermark`, `TextBoxHelper.ClearTextButton`, `ControlHelper.CornerRadius` m.fl. |

## Demo

```bash
dotnet run --project src/JetPack.Wpf.Demo
```

`--snapshot <mappe>` renderer hver side til PNG og lukker igen. Billederne i `docs/screenshots` er lavet på den måde.

## Struktur

```
src/JetPack.Wpf/
  Assets/            tekstur og vignet fra Silverlight-temaet
  Styles/            Brushes, Fonts, Effects, Accents/*, Controls.*.xaml, JetPack.xaml
  Themes/Generic.xaml
  Controls/          egne kontroller
  Helpers/           attached properties og konvertere
  Theming/           ThemeManager og AccentGenerator
src/JetPack.Wpf.Demo/
design/              HTML-udkastet, som udtrykket blev godkendt ud fra
```

## Licens

Koden er under MIT-licensen (se [LICENSE](LICENSE)). Det visuelle design og tekstur/vignet stammer fra Microsofts Silverlight 4 Theme Pack; se [NOTICE.md](NOTICE.md).

## Begrænsninger i version 0.1

- TabControl understøtter kun faner øverst.
- DatePickerens vandmærke ("Select a date") kommer fra WPF's egne tekster og følger ikke sproget.
- Indbygget diagram findes ikke; temaet leverer paletten `JetPack.Brushes.Chart.1–6` til et diagrambibliotek.
