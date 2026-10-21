---
title: Resource keys
parent: Reference
nav_order: 1
---

# Resource keys

Every brush, colour, font size and effect the theme defines, generated from the XAML in
`src/JetPack.Wpf/Styles`. Reference them with `{DynamicResource ...}` so they follow accent changes.

To change a value app-wide, redefine the key in `App.xaml` after merging `JetPack.xaml`, for example
`<SolidColorBrush x:Key="JetPack.Brushes.Error" Color="#FFC00000" />`.

## Accent (changes with ThemeManager.ChangeAccent)

Values shown are the default green accent. `Accents/Orange.xaml` and `Accents/Blue.xaml` define the same keys.

| Key | Type | Value |
| --- | --- | --- |
| `JetPack.Accent.Name` | String | `Green` |
| `JetPack.Colors.Accent` | Color | `#FF75A20A` |
| `JetPack.Colors.Accent.Highlight` | Color | `#FFC2DF00` |
| `JetPack.Colors.Accent.TextShadow` | Color | `#FF2B3D00` |
| `JetPack.Brushes.Accent.Selection` | LinearGradientBrush | `#FF98B913` 0 → `#FF75A20A` 1 |
| `JetPack.Brushes.Accent` | LinearGradientBrush | `#FFBCD742` 0 → `#FF91B006` 1 |
| `JetPack.Brushes.Accent.MouseOver` | LinearGradientBrush | `#FF9EBF0A` 0 → `#FFC3E040` 1 |
| `JetPack.Brushes.Accent.Pressed` | LinearGradientBrush | `#FF6D9000` 0.31 → `#FF5A8100` 0.78 |
| `JetPack.Brushes.Accent.Highlight` | SolidColorBrush | `#FFC2DF00` |
| `JetPack.Brushes.Accent.Solid` | SolidColorBrush | `#FF75A20A` |
| `JetPack.Brushes.Accent.Link` | SolidColorBrush | `#FFD0FF00` |
| `JetPack.Brushes.Accent.Foreground` | SolidColorBrush | `#FFFFFFFF` |
| `JetPack.Brushes.Accent.Button.Normal` | LinearGradientBrush | `#FFA0BB0E` 0.176 → `#FF6D9700` 0.894 |
| `JetPack.Brushes.Accent.Button.MouseOver` | LinearGradientBrush | `#FFAECC07` 0.228 → `#FF83B100` 0.849 |
| `JetPack.Brushes.Accent.Button.Pressed` | LinearGradientBrush | `#FF6D9000` 0.31 → `#FF5A8100` 0.78 |
| `JetPack.Brushes.Accent.Button.Highlight` | SolidColorBrush | `#FFBED126` |
| `JetPack.Effects.Accent.TextShadow` | DropShadowEffect | Color #FF2B3D00, BlurRadius 0, ShadowDepth 1, Direction 90, Opacity 0.5 |
| `JetPack.Brushes.Focus` | SolidColorBrush | `#FFC2DF00` |
| `JetPack.Brushes.Focus.Border` | SolidColorBrush | `#FF75A20A` |

## Neutral brushes

Shared by every accent.

| Key | Type | Value |
| --- | --- | --- |
| `JetPack.Brushes.Window.Background` | ImageBrush | `Texture.png` |
| `JetPack.Brushes.Window.Vignette` | ImageBrush | `Vignette.png` |
| `JetPack.Brushes.Window.Ground` | SolidColorBrush | `#FF86888D` |
| `JetPack.Brushes.Window.Glow` | LinearGradientBrush | `#30FFFFFF` 0 → `#00FFFFFF` 0.5 |
| `JetPack.Brushes.Window.Title.Dark` | LinearGradientBrush | `#FF606066` 0 → `#FF4D4D51` 1 |
| `JetPack.Brushes.Window.Border` | SolidColorBrush | `#FF3B3C40` |
| `JetPack.Brushes.Text` | SolidColorBrush | `#FFFFFFFF` |
| `JetPack.Brushes.Text.Soft` | SolidColorBrush | `#CCFFFFFF` |
| `JetPack.Brushes.Text.Dim` | SolidColorBrush | `#80FFFFFF` |
| `JetPack.Brushes.Text.Disabled` | SolidColorBrush | `#73FFFFFF` |
| `JetPack.Brushes.Header.Foreground` | SolidColorBrush | `#FF303030` |
| `JetPack.Brushes.Control.Foreground` | SolidColorBrush | `#FF202020` |
| `JetPack.Brushes.Control.Foreground.Disabled` | SolidColorBrush | `#80202020` |
| `JetPack.Brushes.Control.Background` | LinearGradientBrush | `#FFF7F7F7` 0.164 → `#FFC6C6C6` 0.802 → `#FFC6C6C6` 0.953 |
| `JetPack.Brushes.Control.Background.MouseOver` | LinearGradientBrush | `#FFFFFFFF` 0.211 → `#FFDEDEDE` 0.784 |
| `JetPack.Brushes.Control.Background.Pressed` | LinearGradientBrush | `#FFCECECE` 0.204 → `#FFAFAFAF` 0.864 |
| `JetPack.Brushes.Control.Background.Checked` | LinearGradientBrush | `#FFE6E6E6` 0.1 → `#FFC9C9C9` 0.9 |
| `JetPack.Brushes.Control.Border.Checked` | SolidColorBrush | `#D0494A4D` |
| `JetPack.Brushes.Control.Border` | SolidColorBrush | `#FF686868` |
| `JetPack.Brushes.Control.Highlight` | SolidColorBrush | `#80FFFFFF` |
| `JetPack.Brushes.Control.OuterHighlight` | SolidColorBrush | `#40FFFFFF` |
| `JetPack.Brushes.Control.DarkEdge` | SolidColorBrush | `#FF2E2E30` |
| `JetPack.Brushes.Control.Glyph` | SolidColorBrush | `#FF606060` |
| `JetPack.Brushes.Control.Glyph.Dark` | SolidColorBrush | `#FF404040` |
| `JetPack.Brushes.Static.Background` | LinearGradientBrush | `#FFE2E2E2` 0 → `#FFC6C6C6` 1 |
| `JetPack.Brushes.Static.Background.MouseOver` | LinearGradientBrush | `#FFF8F8F8` 0 → `#FFD0D0D0` 1 |
| `JetPack.Brushes.Disabled` | LinearGradientBrush | `#FF9C9DA0` 0 → `#FF8B8C8E` 1 |
| `JetPack.Brushes.Disabled.Border` | SolidColorBrush | `#66000000` |
| `JetPack.Brushes.Input.Background` | SolidColorBrush | `#FFFFFFFF` |
| `JetPack.Brushes.Input.Foreground` | SolidColorBrush | `#FF404040` |
| `JetPack.Brushes.Input.Watermark` | SolidColorBrush | `#FF9B9B9B` |
| `JetPack.Brushes.Input.ReadOnly` | SolidColorBrush | `#2E000000` |
| `JetPack.Brushes.Input.Disabled` | SolidColorBrush | `#59909194` |
| `JetPack.Brushes.Input.InnerShadowDark` | SolidColorBrush | `#50505050` |
| `JetPack.Brushes.Input.InnerShadowLight` | SolidColorBrush | `#20202020` |
| `JetPack.Brushes.Input.SelectionForeground` | SolidColorBrush | `#FFFFFFFF` |
| `JetPack.Brushes.Dark.Input.Background` | LinearGradientBrush | `#FF57575F` 0.3 → `#FF505054` 1.0 |
| `JetPack.Brushes.Dark.Input.Border` | SolidColorBrush | `#D0505050` |
| `JetPack.Brushes.Dark.Input.Foreground` | SolidColorBrush | `#FFFFFFFF` |
| `JetPack.Brushes.Dark.Input.Watermark` | SolidColorBrush | `#73FFFFFF` |
| `JetPack.Brushes.Dark.Input.InnerShadowDark` | SolidColorBrush | `#50303030` |
| `JetPack.Brushes.Dark.Input.InnerShadowLight` | SolidColorBrush | `#20303030` |
| `JetPack.Brushes.Dark.Input.ReadOnly` | SolidColorBrush | `#20FFFFFF` |
| `JetPack.Brushes.Dark.Input.Disabled` | SolidColorBrush | `#33FFFFFF` |
| `JetPack.Brushes.Error` | SolidColorBrush | `#FFEF0A0A` |
| `JetPack.Brushes.Error.Tip` | SolidColorBrush | `#FFDC000C` |
| `JetPack.Brushes.Error.Header` | LinearGradientBrush | `#FFF04A4A` 0 → `#FFC41212` 1 |
| `JetPack.Brushes.Track` | SolidColorBrush | `#FF57575C` |
| `JetPack.Brushes.Track.Gradient` | LinearGradientBrush | `#FF57575C` 0.3 → `#FF505054` 1.0 |
| `JetPack.Brushes.Track.Gradient.Vertical` | LinearGradientBrush | `#FF57575C` 0.3 → `#FF505054` 1.0 |
| `JetPack.Brushes.Track.Border` | SolidColorBrush | `#D0505050` |
| `JetPack.Brushes.Slider.Value` | LinearGradientBrush | `#FFCCCCCC` 0 → `#FF9F9F9F` 1 |
| `JetPack.Brushes.Slider.Value.Vertical` | LinearGradientBrush | `#FFCCCCCC` 0 → `#FF9F9F9F` 1 |
| `JetPack.Brushes.Slider.Value.Border` | LinearGradientBrush | `#FF38383C` 0 → `#FF424247` 1 |
| `JetPack.Brushes.Slider.Value.Highlight` | LinearGradientBrush | `#FFF1F1F1` 0 → `#FF969696` 1 |
| `JetPack.Brushes.Slider.Thumb` | LinearGradientBrush | `#FFFDFDFD` 0 → `#FF9D9D9E` 1 |
| `JetPack.Brushes.Slider.Thumb.Vertical` | LinearGradientBrush | `#FFFDFDFD` 0 → `#FF9D9D9E` 1 |
| `JetPack.Brushes.Slider.Thumb.Border` | LinearGradientBrush | `#FFFDFDFD` 0 → `#FF868788` 1 |
| `JetPack.Brushes.CheckBox.Background` | LinearGradientBrush | `#FF57575C` 0.3 → `#FF55555A` 0.5 → `#FF4F4F54` 1.0 |
| `JetPack.Brushes.CheckBox.Border` | SolidColorBrush | `#FF343537` |
| `JetPack.Brushes.CheckBox.InnerShadow` | SolidColorBrush | `#50303030` |
| `JetPack.Brushes.RadioButton.Background` | LinearGradientBrush | `#FF505050` 0.3 → `#FF484848` 1.0 |
| `JetPack.Brushes.RadioButton.InnerShadow` | RadialGradientBrush | `#00000000` 0.5 → `#28000000` 1.0 |
| `JetPack.Brushes.Toggle.Disabled` | SolidColorBrush | `#FF8E8F93` |
| `JetPack.Brushes.Toggle.Disabled.Border` | SolidColorBrush | `#FF717276` |
| `JetPack.Brushes.Toggle.Disabled.Glyph` | SolidColorBrush | `#FFC7C8CA` |
| `JetPack.Brushes.Selection.MouseOver` | LinearGradientBrush | `#FFEDEDEE` 0 → `#FFDEDEDF` 1 |
| `JetPack.Brushes.ScrollBar.Track` | SolidColorBrush | `#FFDCDCDD` |
| `JetPack.Brushes.ScrollBar.Thumb` | LinearGradientBrush | `#FFF8F8F8` 0.1 → `#FFC0C0C0` 1.0 |
| `JetPack.Brushes.ScrollBar.Thumb.Vertical` | LinearGradientBrush | `#FFF8F8F8` 0.1 → `#FFC0C0C0` 1.0 |
| `JetPack.Brushes.Panel.Background` | SolidColorBrush | `#FF707176` |
| `JetPack.Brushes.Panel.Border` | SolidColorBrush | `#FF58585D` |
| `JetPack.Brushes.Strip.Background` | SolidColorBrush | `#FF5B5C61` |
| `JetPack.Brushes.Navigation.Inactive` | SolidColorBrush | `#FF58585D` |
| `JetPack.Brushes.Tab.Inactive.Foreground` | SolidColorBrush | `#E0FFFFFF` |
| `JetPack.Brushes.DataGrid.Header` | LinearGradientBrush | `#FF606066` 0 → `#FF4D4D51` 1 |
| `JetPack.Brushes.DataGrid.Background` | SolidColorBrush | `#FFF0F0F0` |
| `JetPack.Brushes.DataGrid.AlternatingRow` | SolidColorBrush | `#FFE0E0E0` |
| `JetPack.Brushes.DataGrid.GridLines` | SolidColorBrush | `#20000000` |
| `JetPack.Brushes.DataGrid.Header.Separator` | SolidColorBrush | `#20FFFFFF` |
| `JetPack.Brushes.Menu.Background` | SolidColorBrush | `#FFE0E0E0` |
| `JetPack.Brushes.Menu.Disabled` | SolidColorBrush | `#FF9A9A9A` |
| `JetPack.Brushes.Dialog.Background` | SolidColorBrush | `#FFE0E0E0` |
| `JetPack.Brushes.Dialog.Overlay` | SolidColorBrush | `#7F000000` |
| `JetPack.Brushes.Overlay` | LinearGradientBrush | `#4CFFFFFF` 0 → `#4C000000` 1 |
| `JetPack.Brushes.Close.MouseOver` | LinearGradientBrush | `#FFE84B4B` 0 → `#FFB71C1C` 1 |
| `JetPack.Brushes.Button.Green.Normal` | LinearGradientBrush | `#FFA0BB0E` 0.176 → `#FF6D9700` 0.894 |
| `JetPack.Brushes.Button.Green.MouseOver` | LinearGradientBrush | `#FFAECC07` 0.228 → `#FF83B100` 0.849 |
| `JetPack.Brushes.Button.Green.Pressed` | LinearGradientBrush | `#FF6D9000` 0.31 → `#FF5A8100` 0.78 |
| `JetPack.Brushes.Button.Green.Highlight` | SolidColorBrush | `#FFBED126` |
| `JetPack.Colors.Button.Green.TextShadow` | Color | `#FF2B3D00` |
| `JetPack.Brushes.Button.Orange.Normal` | LinearGradientBrush | `#FFFFB400` 0.169 → `#FFFFA700` 0.482 → `#FFFF7600` 0.864 |
| `JetPack.Brushes.Button.Orange.MouseOver` | LinearGradientBrush | `#FFFFC740` 0.254 → `#FFFFB400` 0.496 → `#FFFF9900` 1 |
| `JetPack.Brushes.Button.Orange.Pressed` | LinearGradientBrush | `#FFDD870D` 0.207 → `#FFCE5300` 0.858 |
| `JetPack.Brushes.Button.Orange.Highlight` | SolidColorBrush | `#FFFFCC00` |
| `JetPack.Colors.Button.Orange.TextShadow` | Color | `#FFB62F00` |
| `JetPack.Brushes.Button.Blue.Normal` | LinearGradientBrush | `#FF99C7E2` 0.14 → `#FF87BDDD` 0.375 → `#FF4599CA` 0.871 |
| `JetPack.Brushes.Button.Blue.MouseOver` | LinearGradientBrush | `#FF81CDF8` 0.237 → `#FF2DACF3` 0.866 |
| `JetPack.Brushes.Button.Blue.Pressed` | LinearGradientBrush | `#FF769BB1` 0.22 → `#FF437694` 0.797 |
| `JetPack.Brushes.Button.Blue.Highlight` | SolidColorBrush | `#FFB9DAEC` |
| `JetPack.Colors.Button.Blue.TextShadow` | Color | `#FF00538E` |
| `JetPack.Brushes.Info` | SolidColorBrush | `#FF148CD1` |
| `JetPack.Brushes.Success` | SolidColorBrush | `#FF75A20A` |
| `JetPack.Brushes.Warning` | SolidColorBrush | `#FFFE770A` |
| `JetPack.Brushes.Chart.1` | RadialGradientBrush | `#FFB2C60D` 0 → `#FF4C7B00` 1 |
| `JetPack.Brushes.Chart.2` | RadialGradientBrush | `#FF39B7FF` 0 → `#FF0065AD` 1 |
| `JetPack.Brushes.Chart.3` | RadialGradientBrush | `#FF85878C` 0 → `#FF33353A` 1 |
| `JetPack.Brushes.Chart.4` | RadialGradientBrush | `#FFFF9600` 0 → `#FF942B00` 1 |
| `JetPack.Brushes.Chart.5` | RadialGradientBrush | `#FFEE6028` 0 → `#FF8B0000` 1 |
| `JetPack.Brushes.Chart.6` | RadialGradientBrush | `#FFFFC12B` 0 → `#FFFF7600` 1 |

## Fonts

`JetPack.Font.Size.Control` is what `ThemeManager.ControlFontSize` overrides.

| Key | Type | Value |
| --- | --- | --- |
| `JetPack.Fonts.Family` | FontFamily | `Trebuchet MS, Segoe UI` |
| `JetPack.Fonts.Family.Heading` | FontFamily | `Trebuchet MS, Segoe UI` |
| `JetPack.Fonts.Family.Mono` | FontFamily | `Consolas, Cascadia Mono, Courier New` |
| `JetPack.Font.Size.Control` | Double | `12` |
| `JetPack.Font.Size.Small` | Double | `11` |
| `JetPack.Font.Size.Default` | Double | `12.5` |
| `JetPack.Font.Size.Content` | Double | `13` |
| `JetPack.Font.Size.Heading1` | Double | `36` |
| `JetPack.Font.Size.Heading2` | Double | `24` |
| `JetPack.Font.Size.Heading3` | Double | `21` |
| `JetPack.Font.Size.Heading4` | Double | `18` |
| `JetPack.Font.Size.Heading5` | Double | `15` |
| `JetPack.Font.Size.Heading6` | Double | `11` |
| `JetPack.Font.Size.Navigation` | Double | `15` |

## Effects

Drop shadows and text effects used by the templates.

| Key | Type | Value |
| --- | --- | --- |
| `JetPack.Effects.ControlShadow` | DropShadowEffect | Color #FF000000, BlurRadius 3, ShadowDepth 1, Direction 270, Opacity 0.55 |
| `JetPack.Effects.LightControlShadow` | DropShadowEffect | Color #FF55524F, BlurRadius 4, ShadowDepth 1, Direction 315, Opacity 0.5 |
| `JetPack.Effects.PopupShadow` | DropShadowEffect | Color #FF000000, BlurRadius 5, ShadowDepth 2, Direction 270, Opacity 0.45 |
| `JetPack.Effects.DialogShadow` | DropShadowEffect | Color #FF000000, BlurRadius 24, ShadowDepth 6, Direction 270, Opacity 0.5 |
| `JetPack.Effects.TextEmboss` | DropShadowEffect | Color #FFFFFFFF, BlurRadius 0, ShadowDepth 1, Direction 270, Opacity 0.5 |
| `JetPack.Effects.TextGlow` | DropShadowEffect | Color #FF000000, BlurRadius 5, ShadowDepth 0, Direction 293, Opacity 0.25 |
| `JetPack.Effects.Button.Green.TextShadow` | DropShadowEffect | Color #FF2B3D00, BlurRadius 0, ShadowDepth 1, Direction 90, Opacity 0.5 |
| `JetPack.Effects.Button.Orange.TextShadow` | DropShadowEffect | Color #FFB62F00, BlurRadius 0, ShadowDepth 1, Direction 90, Opacity 0.5 |
| `JetPack.Effects.Button.Blue.TextShadow` | DropShadowEffect | Color #FF00538E, BlurRadius 0, ShadowDepth 1, Direction 90, Opacity 0.5 |

## Named styles

Every standard control also gets an implicit style (no key needed). These keyed styles are the
variants and building blocks; use them with `Style="{StaticResource ...}"` or as `BasedOn`.

| Key | Target type | File |
| --- | --- | --- |
| `JetPack.Styles.Button` | `Button` | Controls.Buttons.xaml |
| `JetPack.Styles.Button.Accent` | `Button` | Controls.Buttons.xaml |
| `JetPack.Styles.Button.Blue` | `Button` | Controls.Buttons.xaml |
| `JetPack.Styles.Button.Flat` | `ButtonBase` | Controls.Buttons.xaml |
| `JetPack.Styles.Button.Green` | `Button` | Controls.Buttons.xaml |
| `JetPack.Styles.Button.Link` | `Button` | Controls.Buttons.xaml |
| `JetPack.Styles.Button.Orange` | `Button` | Controls.Buttons.xaml |
| `JetPack.Styles.Button.Square` | `Button` | Controls.Buttons.xaml |
| `JetPack.Styles.ButtonBase` | `ButtonBase` | Controls.Buttons.xaml |
| `JetPack.Styles.Hyperlink` | `Hyperlink` | Controls.Buttons.xaml |
| `JetPack.Styles.RepeatButton` | `RepeatButton` | Controls.Buttons.xaml |
| `JetPack.Styles.ToggleButton` | `ToggleButton` | Controls.Buttons.xaml |
| `JetPack.Styles.Calendar` | `Calendar` | Controls.Calendar.xaml |
| `JetPack.Styles.Calendar.HeaderButton` | `Button` | Controls.Calendar.xaml |
| `JetPack.Styles.Calendar.NavButton` | `Button` | Controls.Calendar.xaml |
| `JetPack.Styles.CalendarButton` | `CalendarButton` | Controls.Calendar.xaml |
| `JetPack.Styles.CalendarDayButton` | `CalendarDayButton` | Controls.Calendar.xaml |
| `JetPack.Styles.CalendarItem` | `CalendarItem` | Controls.Calendar.xaml |
| `JetPack.Styles.DatePicker` | `DatePicker` | Controls.Calendar.xaml |
| `JetPack.Styles.DatePicker.Button` | `Button` | Controls.Calendar.xaml |
| `JetPack.Styles.DatePickerTextBox` | `DatePickerTextBox` | Controls.Calendar.xaml |
| `JetPack.Styles.Expander` | `Expander` | Controls.Containers.xaml |
| `JetPack.Styles.Expander.Accordion` | `Expander` | Controls.Containers.xaml |
| `JetPack.Styles.Expander.Header` | `ToggleButton` | Controls.Containers.xaml |
| `JetPack.Styles.GridSplitter` | `GridSplitter` | Controls.Containers.xaml |
| `JetPack.Styles.GroupBox` | `GroupBox` | Controls.Containers.xaml |
| `JetPack.Styles.Separator` | `Separator` | Controls.Containers.xaml |
| `JetPack.Styles.StatusBar` | `StatusBar` | Controls.Containers.xaml |
| `JetPack.Styles.StatusBarItem` | `StatusBarItem` | Controls.Containers.xaml |
| `JetPack.Styles.TabControl` | `TabControl` | Controls.Containers.xaml |
| `JetPack.Styles.TabControl.Navigation` | `TabControl` | Controls.Containers.xaml |
| `JetPack.Styles.TabControl.Strip` | `TabControl` | Controls.Containers.xaml |
| `JetPack.Styles.TabItem` | `TabItem` | Controls.Containers.xaml |
| `JetPack.Styles.TabItem.Navigation` | `TabItem` | Controls.Containers.xaml |
| `JetPack.Styles.TabItem.Strip` | `TabItem` | Controls.Containers.xaml |
| `JetPack.Styles.ToolTip` | `ToolTip` | Controls.Containers.xaml |
| `JetPack.Styles.DataGrid` | `DataGrid` | Controls.DataGrid.xaml |
| `JetPack.Styles.DataGrid.Gripper` | `Thumb` | Controls.DataGrid.xaml |
| `JetPack.Styles.DataGrid.SelectAll` | `Button` | Controls.DataGrid.xaml |
| `JetPack.Styles.DataGridCell` | `DataGridCell` | Controls.DataGrid.xaml |
| `JetPack.Styles.DataGridColumnHeader` | `DataGridColumnHeader` | Controls.DataGrid.xaml |
| `JetPack.Styles.DataGridRow` | `DataGridRow` | Controls.DataGrid.xaml |
| `JetPack.Styles.DataGridRowHeader` | `DataGridRowHeader` | Controls.DataGrid.xaml |
| `JetPack.Styles.JetPackGrid` | `c:JetPackGrid` | Controls.DataGrid.xaml |
| `JetPack.Styles.JetPackGrid.ColumnHeader` | `DataGridColumnHeader` | Controls.DataGrid.xaml |
| `JetPack.Styles.JetPackGrid.FilterButton` | `ToggleButton` | Controls.DataGrid.xaml |
| `JetPack.Styles.JetPackGrid.GroupHeader` | `ToggleButton` | Controls.DataGrid.xaml |
| `JetPack.Styles.JetPackGrid.PagerButton` | `Button` | Controls.DataGrid.xaml |
| `JetPack.Styles.Button.Inline` | `ButtonBase` | Controls.JetPack.xaml |
| `JetPack.Styles.ColorPicker.Swatch` | `ListBoxItem` | Controls.JetPack.xaml |
| `JetPack.Styles.DropDownButton.Accent` | `c:DropDownButton` | Controls.JetPack.xaml |
| `JetPack.Styles.Flyout.Close` | `Button` | Controls.JetPack.xaml |
| `JetPack.Styles.HamburgerMenu.Toggle` | `ToggleButton` | Controls.JetPack.xaml |
| `JetPack.Styles.ListBox.Popup` | `ListBox` | Controls.JetPack.xaml |
| `JetPack.Styles.MenuButtonBase` | `ButtonBase` | Controls.JetPack.xaml |
| `JetPack.Styles.NumericUpDown.Dark` | `c:NumericUpDown` | Controls.JetPack.xaml |
| `JetPack.Styles.SplitButton.Accent` | `c:SplitButton` | Controls.JetPack.xaml |
| `JetPack.Styles.SplitButton.Arrow` | `ToggleButton` | Controls.JetPack.xaml |
| `JetPack.Styles.Tile` | `c:Tile` | Controls.JetPack.xaml |
| `JetPack.Styles.Tile.Accent` | `c:Tile` | Controls.JetPack.xaml |
| `JetPack.Styles.Tile.Blue` | `c:Tile` | Controls.JetPack.xaml |
| `JetPack.Styles.Tile.Green` | `c:Tile` | Controls.JetPack.xaml |
| `JetPack.Styles.Tile.Orange` | `c:Tile` | Controls.JetPack.xaml |
| `JetPack.Styles.ComboBox` | `ComboBox` | Controls.Lists.xaml |
| `JetPack.Styles.ComboBox.EditableTextBox` | `TextBox` | Controls.Lists.xaml |
| `JetPack.Styles.ComboBox.Toggle` | `ToggleButton` | Controls.Lists.xaml |
| `JetPack.Styles.ComboBox.Toggle.Editable` | `ToggleButton` | Controls.Lists.xaml |
| `JetPack.Styles.ComboBoxItem` | `ComboBoxItem` | Controls.Lists.xaml |
| `JetPack.Styles.GridViewColumnHeader` | `GridViewColumnHeader` | Controls.Lists.xaml |
| `JetPack.Styles.ListBox` | `ListBox` | Controls.Lists.xaml |
| `JetPack.Styles.ListBoxItem` | `ListBoxItem` | Controls.Lists.xaml |
| `JetPack.Styles.ListView` | `ListView` | Controls.Lists.xaml |
| `JetPack.Styles.ListViewItem` | `ListViewItem` | Controls.Lists.xaml |
| `JetPack.Styles.ScrollBar` | `ScrollBar` | Controls.Lists.xaml |
| `JetPack.Styles.ScrollBar.Arrow` | `RepeatButton` | Controls.Lists.xaml |
| `JetPack.Styles.ScrollViewer` | `ScrollViewer` | Controls.Lists.xaml |
| `JetPack.Styles.ScrollViewer.GridView` | `ScrollViewer` | Controls.Lists.xaml |
| `JetPack.Styles.TreeView` | `TreeView` | Controls.Lists.xaml |
| `JetPack.Styles.TreeViewItem` | `TreeViewItem` | Controls.Lists.xaml |
| `JetPack.Styles.TreeViewItem.Expander` | `ToggleButton` | Controls.Lists.xaml |
| `JetPack.Styles.ContextMenu` | `ContextMenu` | Controls.Menu.xaml |
| `JetPack.Styles.Menu` | `Menu` | Controls.Menu.xaml |
| `JetPack.Styles.MenuItem` | `MenuItem` | Controls.Menu.xaml |
| `JetPack.Styles.ToolBar` | `ToolBar` | Controls.Menu.xaml |
| `JetPack.Styles.ToolBar.Overflow` | `ToggleButton` | Controls.Menu.xaml |
| `JetPack.Styles.ToolBar.Thumb` | `Thumb` | Controls.Menu.xaml |
| `JetPack.Styles.ToolBarTray` | `ToolBarTray` | Controls.Menu.xaml |
| `JetPack.Styles.ProgressBar` | `ProgressBar` | Controls.Range.xaml |
| `JetPack.Styles.Slider` | `Slider` | Controls.Range.xaml |
| `JetPack.Styles.Slider.Thumb.Horizontal` | `Thumb` | Controls.Range.xaml |
| `JetPack.Styles.Slider.Thumb.Vertical` | `Thumb` | Controls.Range.xaml |
| `JetPack.Styles.Slider.Value.Horizontal` | `RepeatButton` | Controls.Range.xaml |
| `JetPack.Styles.Slider.Value.Vertical` | `RepeatButton` | Controls.Range.xaml |
| `JetPack.Styles.Button.Clear` | `Button` | Controls.Shared.xaml |
| `JetPack.Styles.RepeatButton.Transparent` | `RepeatButton` | Controls.Shared.xaml |
| `JetPack.Styles.Label` | `Label` | Controls.Text.xaml |
| `JetPack.Styles.PasswordBox` | `PasswordBox` | Controls.Text.xaml |
| `JetPack.Styles.PasswordBox.Dark` | `PasswordBox` | Controls.Text.xaml |
| `JetPack.Styles.RichTextBox` | `RichTextBox` | Controls.Text.xaml |
| `JetPack.Styles.TextBlock` | `TextBlock` | Controls.Text.xaml |
| `JetPack.Styles.TextBlock.Caption` | `TextBlock` | Controls.Text.xaml |
| `JetPack.Styles.TextBlock.Content` | `TextBlock` | Controls.Text.xaml |
| `JetPack.Styles.TextBlock.Heading` | `TextBlock` | Controls.Text.xaml |
| `JetPack.Styles.TextBlock.Heading1` | `TextBlock` | Controls.Text.xaml |
| `JetPack.Styles.TextBlock.Heading2` | `TextBlock` | Controls.Text.xaml |
| `JetPack.Styles.TextBlock.Heading3` | `TextBlock` | Controls.Text.xaml |
| `JetPack.Styles.TextBlock.Heading4` | `TextBlock` | Controls.Text.xaml |
| `JetPack.Styles.TextBlock.Heading5` | `TextBlock` | Controls.Text.xaml |
| `JetPack.Styles.TextBlock.Heading6` | `TextBlock` | Controls.Text.xaml |
| `JetPack.Styles.TextBox` | `TextBox` | Controls.Text.xaml |
| `JetPack.Styles.TextBox.Dark` | `TextBox` | Controls.Text.xaml |
| `JetPack.Styles.TextBox.MultiLine` | `TextBox` | Controls.Text.xaml |
| `JetPack.Styles.TextBox.MultiLine.Dark` | `TextBox` | Controls.Text.xaml |
| `JetPack.Styles.TextBoxBase` | `TextBoxBase` | Controls.Text.xaml |
| `JetPack.Styles.CheckBox` | `CheckBox` | Controls.Toggles.xaml |
| `JetPack.Styles.CheckBox.OnLight` | `CheckBox` | Controls.Toggles.xaml |
| `JetPack.Styles.RadioButton` | `RadioButton` | Controls.Toggles.xaml |
| `JetPack.Styles.RadioButton.OnLight` | `RadioButton` | Controls.Toggles.xaml |
| `JetPack.Styles.Button.WindowCaption` | `Button` | Controls.Window.xaml |
| `JetPack.Styles.Button.WindowClose` | `Button` | Controls.Window.xaml |
| `JetPack.Styles.Button.WindowCommand` | `ButtonBase` | Controls.Window.xaml |
| `JetPack.Styles.Window` | `Window` | Controls.Window.xaml |
