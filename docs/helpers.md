---
title: Helpers and custom templates
parent: Reference
nav_order: 2
---

# Helpers and custom templates

## ControlHelper

Attached properties the JetPack templates read, so one template serves several looks. Set them in your
own styles to recolour a control without copying its template (see the example in
[Buttons](buttons.md#custom-colours)).

| Attached property | Type | Read by |
| --- | --- | --- |
| `ControlHelper.CornerRadius` | `CornerRadius` (default 2) | buttons, toggle buttons, expander headers |
| `ControlHelper.MouseOverBackground` | `Brush` | buttons, tiles, split/drop-down buttons, navigation tabs |
| `ControlHelper.PressedBackground` | `Brush` | buttons, tiles, split/drop-down buttons |
| `ControlHelper.HighlightBrush` | `Brush` | the 1px top/left highlight on raised surfaces |
| `ControlHelper.ContentEffect` | `Effect` | effect on the content: embossed or shadowed text |
| `ControlHelper.EdgeBrush` | `Brush` | dark top/left edge of the dark inputs |
| `ControlHelper.InnerShadowDark`, `InnerShadowLight` | `Brush` | the two inset lines of inputs |
| `ControlHelper.ReadOnlyBrush` | `Brush` | overlay on read-only inputs |
| `ControlHelper.DisabledBrush` | `Brush` | overlay on disabled inputs |
| `ControlHelper.InputStyle` | `Style` | the text box inside `NumericUpDown`, `TimePicker`, `AutoSuggestBox` |
| `ControlHelper.HeaderForeground` | `Brush` | tab header text (kept off the `TabItem` so page content does not inherit it) |

## TextBoxHelper

| Member | Type | Description |
| --- | --- | --- |
| `Watermark` | attached `string` | Hint shown while empty (`TextBox`, `PasswordBox`, editable `ComboBox`, composite inputs). |
| `ClearTextButton` | attached `bool` | Shows the × button while there is text. |
| `HasText` | attached `bool`, read-only | Kept up to date while `IsMonitoring` is on. |
| `IsMonitoring` | attached `bool` | Tracks text changes and handles `ClearTextCommand`. The JetPack styles turn it on; set it yourself only in a style that does not derive from them. |
| `ClearTextCommand` | `RoutedUICommand` | Clears the text box it is raised in. |

## Converters

| Key | Class | Converts |
| --- | --- | --- |
| `JetPack.Converters.ColorToBrush` | `ColorToBrushConverter` | `Color` → frozen `SolidColorBrush` (and back) |
| `JetPack.Converters.ColorToHex` | `ColorToHexConverter` | `Color` → `#RRGGBB` / `#AARRGGBB` (and back) |

## Reusable resources

| Key | What |
| --- | --- |
| `JetPack.Styles.FocusVisual` | the 1px accent focus ring |
| `JetPack.Templates.ValidationError` | red frame, folded corner and error call-out |
| `JetPack.Templates.Button`, `JetPack.Templates.ToggleButton` | the button chrome, driven by `ControlHelper` |
| `JetPack.Styles.Button.Inline` | small raised button used inside inputs (spinners, picker buttons) |
| `JetPack.Styles.Button.Clear` | the small × button |
| `JetPack.Styles.ListBox.Popup` | frameless list for your own drop-downs |
| `JetPack.Geometry.Check`, `ArrowDown/Up/Left/Right`, `Close`, `Notch`, `ErrorCorner` | glyph geometries |

## Template parts

When you replace the template of a JetPack control, keep these named parts:

| Control | Parts |
| --- | --- |
| `JetPackWindow` | `PART_Root` (FrameworkElement, gets the maximised margin), `PART_DialogHost` (Grid, hosts dialogs) |
| `JetPackDialog` | `PART_Affirmative`, `PART_Negative`, `PART_Close` (Button) |
| `NumericUpDown` | `PART_TextBox` (TextBox), `PART_Up`, `PART_Down` (ButtonBase) |
| `SplitButton` | `PART_Arrow` (ToggleButton) |
| `Flyout`, `InfoBar` | `PART_Close` (Button) |
| `RangeSlider` | `PART_Track` (Canvas), `PART_Fill` (FrameworkElement), `PART_LowerThumb`, `PART_UpperThumb` (Thumb) |
| `Rating` | `PART_Host` (Panel; the stars are added to it) |
| `TimePicker` | `PART_TextBox` (TextBox), `PART_Popup` (Popup), `PART_List` (ListBox) |
| `ColorPicker` | `PART_Palette` (ListBox) |
| `AutoSuggestBox` | `PART_TextBox` (TextBox), `PART_Popup` (Popup), `PART_List` (ListBox) |
| `BusyIndicator` | `PART_Content` (UIElement; blurred while busy) |

The standard WPF controls keep their usual parts (`PART_ContentHost`, `PART_Track`, `PART_Popup`,
`PART_EditableTextBox`, and so on).

## Notes for custom styles

- Each `Controls.*.xaml` file is loaded on its own. In your own dictionaries, use `DynamicResource`
  for JetPack brushes, and `BasedOn="{StaticResource JetPack.Styles...}"` only where the JetPack
  dictionary is already merged above.
- WPF's built-in theme styles still apply under implicit styles. If your template ignores a property
  the system theme sets (for example `Foreground` on `TreeViewItem`), add
  `<Setter Property="OverridesDefaultStyle" Value="True" />`.
- Implicit styles do not reach subclasses (`MyButton : Button`). Give the subclass a style
  `BasedOn="{StaticResource JetPack.Styles.Button}"`, or set its `DefaultStyleKey`.
