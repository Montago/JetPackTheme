---
title: Window and dialogs
parent: Controls
nav_order: 1
---

# Window and dialogs

![JetPackWindow with the Companies sample](screenshots/window.png)

## JetPackWindow

`JetPack.Wpf.Controls.JetPackWindow` derives from `Window` and draws its own frame with
`WindowChrome`: the tiled grey texture, the vignette, a 30px title bar with icon, title, window
commands and caption buttons, a layer for flyouts and an overlay for dialogs. Snapping, resizing and
the system menu keep working.

```xml
<jp:JetPackWindow x:Class="MyApp.MainWindow"
                  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                  xmlns:jp="urn:jetpack-wpf"
                  Title="Consultant planning" TitleBarMode="Dark" CornerStyle="Rounded">
    <jp:JetPackWindow.RightWindowCommands>
        <StackPanel Orientation="Horizontal">
            <Button Content="Settings" Click="OnSettings" />
            <Button Content="Help" />
        </StackPanel>
    </jp:JetPackWindow.RightWindowCommands>
    ...
</jp:JetPackWindow>
```

### Properties

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `TitleBarMode` | `TitleBarMode` | `Dark` | `Dark`: grey gradient (the data grid header) with white text. `Light`: the light navigation-bar gradient with dark embossed text. `Texture`: no bar; the texture runs to the top. |
| `CornerStyle` | `WindowCornerStyle` | `Square` | `Rounded` asks the Windows 11 compositor for rounded corners and a frame in the JetPack border colour. Windows 10 keeps square corners. |
| `HasSystemRoundedCorners` | `bool` (read-only) | | True while Windows draws rounded corners; the template then leaves out its own 1px border. |
| `TitleBarHeight` | `double` | 30 | Height of the title bar and of the draggable caption area. |
| `ShowIcon` | `bool` | `true` | Shows `Icon` at the left of the title bar. |
| `ShowVignette` | `bool` | `true` | The darkened edges of the original theme. |
| `ShowMinButton` | `bool` | `true` | |
| `ShowMaxRestoreButton` | `bool` | `true` | Also hidden when `ResizeMode="NoResize"`. |
| `LeftWindowCommands` | `object` | `null` | Content after the icon. |
| `RightWindowCommands` | `object` | `null` | Content before the minimise button. |
| `Flyouts` | `object` | `null` | A layer above the window content for `Flyout` panels; usually a `Grid`. |

`Button` and `ToggleButton` inside the window-command areas automatically get the flat title-bar look
(`JetPack.Styles.Button.WindowCommand`) and inherit the title text colour, which follows
`TitleBarMode`.

![Light title bar](screenshots/window-light.png)

### Layout tips

The content area is everything under the title bar. Two layouts that match the original navigation
template:

```xml
<DockPanel>
    <StatusBar DockPanel.Dock="Bottom">
        <StatusBarItem Content="Ready" />
        <StatusBarItem HorizontalAlignment="Right" Content="Last synced 09:42" />
    </StatusBar>
    <TabControl Style="{StaticResource JetPack.Styles.TabControl.Navigation}">
        <TabControl.Tag>
            <TextBlock Margin="0,0,16,0" FontSize="15" FontWeight="Bold" Text="My app" />
        </TabControl.Tag>
        <TabItem Header="Companies">...</TabItem>
    </TabControl>
</DockPanel>
```

![Full-width navigation bar](screenshots/navigation-bar.png)

For the dark tab strip of the original theme browser, use `JetPack.Styles.TabControl.Strip` inside a
margin instead. See [Navigation and layout](navigation.md#tabcontrol).

### Maximised windows

When maximised, the window keeps its content inside the screen's work area (the resize border that
`WindowChrome` hangs over the edge is compensated) and drops its border.

## Flyouts

![Flyout](screenshots/controls/flyout.png)

Put `Flyout` panels in the `Flyouts` layer. They cover the content area from the right (or left) and
slide in when `IsOpen` becomes true:

```xml
<jp:JetPackWindow.Flyouts>
    <Grid>
        <jp:Flyout x:Name="SettingsFlyout" Header="Settings" Width="300">
            <StackPanel>
                <jp:ToggleSwitch Content="Notifications" IsChecked="True" />
            </StackPanel>
        </jp:Flyout>
    </Grid>
</jp:JetPackWindow.Flyouts>
```

```csharp
private void OnSettings(object sender, RoutedEventArgs e) => SettingsFlyout.IsOpen = !SettingsFlyout.IsOpen;
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `IsOpen` | `bool` | `false` | Two-way bindable. |
| `Position` | `FlyoutPosition` | `Right` | `Right` or `Left`; sets the alignment and slide direction. |
| `IsClosable` | `bool` | `true` | Shows the round back button next to the header. |
| `Header` | `object` | | Bold title next to the back button. |

A `Flyout` works in any `Grid`, not only in the window layer: the demo uses one inside a small panel
on the *Extras* page.

![Flyout in the window](screenshots/flyout.png)

## Dialogs

Dialogs are shown inside the window, over a 50% black overlay, in the style of the Silverlight
`ChildWindow`: a light title strip, a grey body and the buttons bottom right.

### ShowMessageAsync

```csharp
var result = await window.ShowMessageAsync(
    "Delete company?",
    "Contoso Corporation and 3 linked placements will be deleted permanently.",
    affirmativeText: "Delete",
    negativeText: "Cancel");

if (result == MessageDialogResult.Affirmative) { ... }
```

`MessageDialogResult` is `Affirmative`, `Negative` or `Canceled` (Escape or the close button). Leave
`negativeText` out for a single OK button.

### JetPackDialog

For your own content or button styles, create a `JetPackDialog` and show it with `ShowDialogAsync`:

```csharp
var dialog = new JetPackDialog
{
    Title = "Delete company?",
    Content = "Contoso Corporation and 3 linked placements will be deleted permanently.",
    AffirmativeText = "Delete",
    NegativeText = "Cancel",
    AffirmativeButtonStyle = (Style)FindResource("JetPack.Styles.Button.Orange"),
};
var result = await window.ShowDialogAsync(dialog);
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Title` | `string` | | Text in the title strip. |
| `Content` | `object` | | String content wraps; any element can be used. |
| `AffirmativeText` | `string` | `OK` | Main button (default button, Enter). |
| `NegativeText` | `string?` | `null` | Second button (cancel button); hidden when null. |
| `AffirmativeButtonStyle` | `Style` | `Button.Accent` | E.g. `JetPack.Styles.Button.Orange` for a destructive action. |

Call `dialog.Close(result)` from your own content to close it with a result. Focus returns to the
element that had it before the dialog opened.

![Dialog](screenshots/dialog.png)

## Plain Window

To keep the system title bar but use the JetPack background and fonts:

```xml
<Window Style="{StaticResource JetPack.Styles.Window}" ... />
```
