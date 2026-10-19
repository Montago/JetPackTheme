# JetPack for WPF

The JetPack theme from Microsoft's Silverlight 4 Application Themes, rebuilt as a complete controls library for WPF on .NET 8 and later. It follows the same model as MahApps.Metro: one resource dictionary gives every standard control the JetPack look, and the library adds its own controls (window, ToggleSwitch, NumericUpDown, Flyout, HamburgerMenu and more).

![Core](docs/screenshots/core.png)

## Getting started

Reference the project (or the `JetPack.Wpf` package) and merge the dictionary in `App.xaml`:

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
                  Title="My app" TitleBarMode="Dark">
    ...
</jp:JetPackWindow>
```

```csharp
public partial class MainWindow : JetPack.Wpf.Controls.JetPackWindow { ... }
```

`JetPackWindow` options:

| Property | Values |
| --- | --- |
| `TitleBarMode` | `Dark` (default), `Light`, `Texture` |
| `CornerStyle` | `Square` (default, as in the original theme) or `Rounded`. Rounded corners are drawn by the Windows 11 compositor; on Windows 10 the corners stay square. |
| `ShowVignette`, `ShowIcon`, `ShowMinButton`, `ShowMaxRestoreButton` | `bool` |
| `LeftWindowCommands`, `RightWindowCommands`, `Flyouts` | any content |

Two common layouts for the window content:

```xml
<DockPanel>
    <!-- footer -->
    <StatusBar DockPanel.Dock="Bottom">
        <StatusBarItem Content="Ready" />
        <StatusBarItem HorizontalAlignment="Right" Content="Last synced 09:42" />
    </StatusBar>
    <!-- full-width grey navigation bar right under the title bar; Tag goes to its right end -->
    <TabControl Style="{StaticResource JetPack.Styles.TabControl.Navigation}">
        <TabControl.Tag>
            <TextBlock FontSize="15" FontWeight="Bold" Text="My app" />
        </TabControl.Tag>
        <TabItem Header="Companies">...</TabItem>
        <TabItem Header="Orders">...</TabItem>
    </TabControl>
</DockPanel>
```

For the dark tab strip of the original theme browser, use `JetPack.Styles.TabControl.Strip` instead
(the demo can switch between the two under *Settings → Page navigation*).

## Theme and accent

```csharp
using JetPack.Wpf.Theming;

ThemeManager.ChangeAccent(JetPackAccent.Orange);            // Green (default), Orange, Blue
ThemeManager.ChangeAccent(Color.FromRgb(0x8E, 0x44, 0xAD)); // generate a full accent from one colour
ThemeManager.ShowFocusVisuals = false;                      // the original's invisible focus
ThemeManager.ControlFontSize = 11;                          // the original's control font size (default: 12)
```

Resource keys follow the MahApps scheme with a `JetPack.` prefix: `JetPack.Brushes.Accent`, `JetPack.Brushes.Accent.Selection`, `JetPack.Brushes.Control.Background`, `JetPack.Styles.Button.Accent` and so on. The *Palette* tab in the demo lists them all.

## What's included

| Group | Controls |
| --- | --- |
| Standard WPF (implicit styles) | Button, RepeatButton, ToggleButton, Hyperlink, TextBox, PasswordBox, RichTextBox, Label, CheckBox, RadioButton, ComboBox, ListBox, ListView/GridView, TreeView, ScrollBar, ScrollViewer, ProgressBar, Slider, ToolTip, GroupBox, Expander, TabControl, Calendar, DatePicker, DataGrid, GridSplitter, Separator, Menu, ContextMenu, ToolBar, StatusBar, validation errors |
| Named variants | `Button.Accent/Green/Orange/Blue/Square/Flat/Link`, `TextBox.Dark`, `TextBox.MultiLine(.Dark)`, `PasswordBox.Dark`, `TabControl.Navigation` (the full-width light link bar with the notch), `TabControl.Strip` (the dark tab strip), `Expander.Accordion`, `TextBlock.Heading1–6` |
| Own controls (`jp:`) | JetPackWindow, JetPackDialog, ToggleSwitch, NumericUpDown, SplitButton, DropDownButton, ProgressRing, Badged, Tile, Flyout, HamburgerMenu, InfoBar, RangeSlider, Rating, TimePicker, ColorPicker, AutoSuggestBox, BusyIndicator, Accordion |
| Helpers | `TextBoxHelper.Watermark`, `TextBoxHelper.ClearTextButton`, `ControlHelper.CornerRadius` and others |

## Demo

```bash
dotnet run --project src/JetPack.Wpf.Demo
```

`--snapshot <folder>` renders every page to PNG and exits. The images in `docs/screenshots` were made that way.

## Layout

```
src/JetPack.Wpf/
  Assets/            texture and vignette from the Silverlight theme
  Styles/            Brushes, Fonts, Effects, Accents/*, Controls.*.xaml, JetPack.xaml
  Themes/Generic.xaml
  Controls/          own controls
  Helpers/           attached properties and converters
  Theming/           ThemeManager and AccentGenerator
src/JetPack.Wpf.Demo/
design/              the HTML draft the look was approved from
```

## License

The code is licensed under the MIT License (see [LICENSE](LICENSE)), **except** `src/JetPack.Wpf/Assets/Texture.png` and `src/JetPack.Wpf/Assets/Vignette.png`. Those two images are copied unchanged from Microsoft's [Silverlight 4 Application Themes](https://www.microsoft.com/en-us/download/details.aspx?id=14590) and belong to Microsoft. Colours and shadow values come from the same theme; all WPF styles are written from scratch. See [NOTICE.md](NOTICE.md) for details.

## Limitations in version 0.1

- TabControl only supports tabs at the top.
- The DatePicker watermark ("Select a date") comes from WPF's own resources and does not follow the app language.
- There is no built-in chart; the theme provides the `JetPack.Brushes.Chart.1–6` palette for a charting library.
