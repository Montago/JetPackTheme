# JetPack for WPF documentation

JetPack for WPF restyles every standard WPF control with the JetPack look from Microsoft's
Silverlight 4 Application Themes and adds the extra controls a desktop app usually needs
(window, dialogs, toggle switch, numeric input, flyout, hamburger menu and more).

![Core controls](screenshots/core.png)

## Start here

- [Getting started](getting-started.md): install, merge the dictionary, first window.
- [Theme and accent](theming.md): accents, generated accents, focus visuals, font size, overriding colours.
- [Typography](typography.md): fonts, heading styles, text rendering.

## Controls

| Page | Controls |
| --- | --- |
| [Window and dialogs](window.md) | `JetPackWindow`, window commands, flyout layer, `JetPackDialog`, `ShowMessageAsync`, plain `Window` style |
| [Buttons](buttons.md) | `Button` and its variants, `ToggleButton`, `RepeatButton`, `Hyperlink`, `SplitButton`, `DropDownButton`, `Tile`, `Badged` |
| [Text and input](inputs.md) | `TextBox`, `PasswordBox`, `RichTextBox`, `Label`, `ComboBox`, `NumericUpDown`, `AutoSuggestBox`, `DatePicker`, `TimePicker`, `ColorPicker`, watermarks, clear button, validation |
| [Selection](selection.md) | `CheckBox`, `RadioButton`, `ToggleSwitch`, `Slider`, `RangeSlider`, `Rating` |
| [Lists and data](lists.md) | `ListBox`, `ListView`/`GridView`, `TreeView`, `DataGrid`, `Calendar`, `ScrollViewer`/`ScrollBar` |
| [Navigation and layout](navigation.md) | `TabControl` (three looks), `HamburgerMenu`, `Expander`, `Accordion`, `GroupBox`, `Menu`, `ContextMenu`, `ToolBar`, `StatusBar`, `GridSplitter`, `Separator`, `ToolTip` |
| [Progress and status](status.md) | `ProgressBar`, `ProgressRing`, `BusyIndicator`, `InfoBar` |

## Reference

- [Resource keys](resource-keys.md): every brush, colour, font size, effect and named style (generated from the XAML).
- [Helpers and custom templates](helpers.md): `ControlHelper`, `TextBoxHelper`, converters, template part names.

## Conventions

- XML namespace: `xmlns:jp="urn:jetpack-wpf"` covers `JetPack.Wpf.Controls`, `JetPack.Wpf.Helpers` and
  `JetPack.Wpf.Theming`.
- Standard controls need nothing: merging `JetPack.xaml` gives them the JetPack style. Variants are
  keyed styles named `JetPack.Styles.<Control>.<Variant>`.
- Use `DynamicResource` for theme brushes in your own XAML, so they follow accent changes.
- The demo app (`src/JetPack.Wpf.Demo`) shows every control on six pages; the screenshots in these
  pages come from it.
