---
title: Getting started
nav_order: 2
---

# Getting started

## Requirements

- .NET 8 or later, Windows (`net8.0-windows`, `UseWPF`).
- Windows 10 or 11. Rounded window corners need Windows 11; everything else works on both.

## Add the library

Install the [JetPack.Wpf](https://www.nuget.org/packages/JetPack.Wpf) package:

```bash
dotnet add package JetPack.Wpf
```

or in the project file:

```xml
<ItemGroup>
  <PackageReference Include="JetPack.Wpf" Version="0.1.0" />
</ItemGroup>
```

To work on the library itself, reference `src/JetPack.Wpf/JetPack.Wpf.csproj` with a
`ProjectReference` instead.

## Merge the theme

Merge one dictionary in `App.xaml`. It brings the brushes, the green accent, fonts, effects and the
styles for every control:

```xml
<Application x:Class="MyApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="pack://application:,,,/JetPack.Wpf;component/Styles/JetPack.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

Add your own resources after the merged dictionary; anything you define with a JetPack key overrides
the theme value (see [Theme and accent](theming.md#overriding-colours)).

## Use JetPackWindow

`JetPackWindow` gives the textured background, the vignette and the JetPack title bar. Derive your
window from it:

```xml
<jp:JetPackWindow x:Class="MyApp.MainWindow"
                  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                  xmlns:jp="urn:jetpack-wpf"
                  Title="Consultant planning" Width="1100" Height="720">
    <DockPanel>
        <StatusBar DockPanel.Dock="Bottom">
            <StatusBarItem Content="Ready" />
        </StatusBar>
        <TabControl Style="{StaticResource JetPack.Styles.TabControl.Navigation}">
            <TabItem Header="Companies">
                <TextBlock Margin="24" Style="{StaticResource JetPack.Styles.TextBlock.Heading1}" Text="Companies" />
            </TabItem>
            <TabItem Header="Orders" />
        </TabControl>
    </DockPanel>
</jp:JetPackWindow>
```

```csharp
using JetPack.Wpf.Controls;

public partial class MainWindow : JetPackWindow
{
    public MainWindow() => InitializeComponent();
}
```

A plain `Window` also works: give it `Style="{StaticResource JetPack.Styles.Window}"` to get the
texture and fonts while keeping the system title bar. See [Window and dialogs](window.md).

## Text colour on the texture

The window sets `Foreground` to white, so text placed directly on the grey texture is white. Controls
with light surfaces (buttons, text boxes, lists, menus) set their own dark text. Dark panels
(`TabControl`, `GroupBox`, `TreeView`, expanders) keep white text.

## Next steps

- Pick an accent and decide on focus visuals: [Theme and accent](theming.md).
- Browse the controls: start from the [documentation index](README.md).
- Run the demo to see every control: `dotnet run --project src/JetPack.Wpf.Demo`.
