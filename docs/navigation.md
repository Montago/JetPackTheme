---
title: Navigation and layout
parent: Controls
grand_parent: JetPack for WPF
nav_order: 6
---

# Navigation and layout

## TabControl

There are three looks.

### Panel tabs (default)

![Panel tabs](screenshots/controls/tabcontrol.png)

The SDK `TabControl`: dark tabs on a dark panel (`#707176`) with a soft shadow. The selected tab
merges into the panel; unselected tabs are a shade darker. Page content is white text.

```xml
<TabControl>
    <TabItem Header="Available">...</TabItem>
    <TabItem Header="Assigned">...</TabItem>
</TabControl>
```

### Navigation bar

![Navigation bar](screenshots/controls/tabcontrol-navigation.png)

`JetPack.Styles.TabControl.Navigation` is the light link bar of the original navigation template:
a full-width grey bar with bold links, the selected link in the accent gradient with a small notch
pointing down at the page. The first link sits flush with the left edge, so put it directly under the
title bar. `Tag` is shown at the right end of the bar (branding, search, user).

```xml
<TabControl Style="{StaticResource JetPack.Styles.TabControl.Navigation}">
    <TabControl.Tag>
        <TextBlock Margin="0,0,16,0" FontSize="15" FontWeight="Bold" Text="My app" />
    </TabControl.Tag>
    <TabItem Header="Companies">...</TabItem>
    <TabItem Header="Orders">...</TabItem>
</TabControl>
```

### Strip

![Tab strip](screenshots/controls/tabcontrol-strip.png)

`JetPack.Styles.TabControl.Strip` is the dark tab strip from the original theme browser (Core / SDK /
Toolkit / Charts), with white links and the same accent notch. `Tag` is shown at the right, outside
the strip. The demo uses it for its pages.

```xml
<TabControl Style="{StaticResource JetPack.Styles.TabControl.Strip}" Margin="24,16,24,0">
    <TabItem Header="Core">...</TabItem>
</TabControl>
```

All three support only `TabStripPlacement="Top"`.

### Notch animation and page transitions

The notch of the navigation bar and the strip moves to the selected tab. With
`jp:TabControlHelper.SelectionAnimation` it slides along the bar and settles with a small overshoot
(`Slide`, the default), pulls up into the bar and drops down under the new tab (`Drop`), or jumps
straight there (`None`).

Pages change at once by default. `jp:TabControlHelper.ContentTransition="Fade"` cross-fades from the
old page to the new one. It works with all three TabControl styles. The notch and the fade start once
the new page has been drawn, so a heavy page does not eat their first frames; until then the old page
stays in place.

```xml
<TabControl Style="{StaticResource JetPack.Styles.TabControl.Strip}"
            jp:TabControlHelper.SelectionAnimation="Drop"
            jp:TabControlHelper.ContentTransition="Fade">
    ...
</TabControl>
```

The demo's settings flyout switches between them.

## HamburgerMenu

![HamburgerMenu](screenshots/controls/hamburgermenu.png)

`jp:HamburgerMenu` is a `ListBox` with a dark pane on the left and a content area on the right. The
selected item gets the accent gradient. The ☰ button collapses the pane to icons.

When the items do not fit, the pane scrolls with the [thin accent bar](lists.md#thin-scroll-bars),
which shows while the mouse is over the pane. The bar then gets a 12px lane beside the items, and the
collapsed pane grows by that much (`CompactPaneWidth` + 12), so the icons keep their place.

```xml
<jp:HamburgerMenu SelectedIndex="0" Content="{Binding SelectedItem.Tag, RelativeSource={RelativeSource Self}}">
    <jp:HamburgerMenu.ContentTemplate>
        <DataTemplate>
            <ContentPresenter Content="{Binding}" />
        </DataTemplate>
    </jp:HamburgerMenu.ContentTemplate>
    <jp:HamburgerMenuItem Content="Overview" Tag="{StaticResource OverviewPage}">
        <jp:HamburgerMenuItem.Icon>
            <jp:JetPackIcon Kind="Home" />
        </jp:HamburgerMenuItem.Icon>
    </jp:HamburgerMenuItem>
</jp:HamburgerMenu>
```

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Content` | `object` | `null` | What the content area shows; usually bound to the selected item. |
| `ContentTemplate` | `DataTemplate` | `null` | |
| `IsPaneOpen` | `bool` | `true` | Two-way bindable; false shows only icons. |
| `PaneWidth` | `double` | 170 | |
| `CompactPaneWidth` | `double` | 40 | |

`HamburgerMenuItem` adds `Icon` (any element, 18×18; a `jp:JetPackIcon` there is drawn at 18×18, where its 24×24 grid scales by 3/4 and stays sharper than at 16). With `ItemsSource`, items are wrapped in
`HamburgerMenuItem` containers automatically.

### Groups

![HamburgerMenu with an open group](screenshots/controls/hamburgermenu-groups.png)

A `HamburgerMenuItem` with `Items` is a group: clicking it opens or closes its sub-items, and it is
never selected itself. Sub-items show no icon and their text lines up with the group's; the group has
the Expander's arrow (right when closed, down when open). There are two levels: sub-items of a
sub-item are ignored.

```xml
<jp:HamburgerMenuItem Content="Planning" IsExpanded="True">
    <jp:HamburgerMenuItem.Icon>
        <jp:JetPackIcon Kind="Calendar" />
    </jp:HamburgerMenuItem.Icon>
    <jp:HamburgerMenuItem.Items>
        <jp:HamburgerMenuItem Content="Week plan" Tag="{StaticResource WeekPlanPage}" />
        <jp:HamburgerMenuItem Content="History" Tag="{StaticResource HistoryPage}" />
    </jp:HamburgerMenuItem.Items>
</jp:HamburgerMenuItem>
```

The menu keeps each group's sub-items right after it in its own `Items`, so `SelectedItem` is the
selected page on either level and a binding such as `SelectedItem.Tag` keeps working. `SelectedIndex`
or `IsSelected` on a group selects its first sub-item, and selecting a sub-item opens its group. A
closed group, or any group while the pane shows only icons, gets the selected look when it holds the
selected page; with the pane showing only icons, clicking a group opens the pane and the group.

Up and Down move over the visible items and select pages but not groups; Right, Left and Enter open
and close a group, and Left on a sub-item goes to its group. Groups need items declared as
`HamburgerMenuItem`s; with `ItemsSource` the menu has one level.

| `HamburgerMenuItem` property | Type | Description |
| --- | --- | --- |
| `Items` | `ObservableCollection<HamburgerMenuItem>` | the sub-items; any makes the item a group |
| `IsExpanded` | `bool` | whether a group shows its sub-items (two-way bindable) |
| `HasItems`, `IsSubItem` | `bool` | read-only: the item is a group, or a sub-item |
| `IsChildSelected` | `bool` | read-only: one of the group's sub-items is selected |
| `ShowsChildSelection` | `bool` | read-only: the group has the selected look in place of its sub-item |

## Expander

![Expanders](screenshots/controls/expander.png)

The header is a raised light bar with a bold label and an arrow that turns down when open; the body is
a dark panel with a soft shadow.

```xml
<Expander Header="Billing address" IsExpanded="True">
    <TextBlock Text="120 Pine Street&#10;Seattle, WA 98101" />
</Expander>
```

`JetPack.Styles.Expander.Accordion` is a flush variant (square header, no gap) used by `Accordion`.
Only `ExpandDirection="Down"` is styled.

![Expanders, dark](screenshots/controls/expander-dark.png)

`JetPack.Styles.Expander.Dark` draws the header in the dark grey of `Button.Dark`;
`JetPack.Styles.Accordion.Dark` does that for every section of an accordion. (The whole app: [Light
and dark](theming.md#light-and-dark).)

## Accordion

![Accordion](screenshots/controls/accordion.png)

`jp:Accordion` stacks expanders in one framed panel; opening one closes the others. Given a fixed or
stretched height, the open section fills the space the closed headers leave, so those headers sit at
the bottom (as in the Toolkit Accordion). With unlimited height it simply stacks.

```xml
<jp:Accordion VerticalAlignment="Stretch">
    <Expander Header="Need Placement" IsExpanded="True">
        <TreeView Background="Transparent" BorderThickness="0" ItemsSource="{Binding Companies}" />
    </Expander>
    <Expander Header="Renewal Pending">...</Expander>
    <Expander Header="Archive">...</Expander>
</jp:Accordion>
```

Items that are not `Expander`s are wrapped in one. The layout comes from `jp:AccordionPanel`, which
you can also use as the `ItemsPanel` of your own items control.

## GroupBox

![GroupBox](screenshots/controls/groupbox.png)

A dark panel with a dark gradient header strip and bold white header text.

```xml
<GroupBox Header="Contact">
    <StackPanel>...</StackPanel>
</GroupBox>
```

## Menu, ContextMenu and ToolBar

![Menu with a drop-down open](screenshots/controls/menu.png)

- `Menu`: a light grey bar with a drop shadow; open and hovered top-level items get the accent gradient.
- Menu drop-downs and `ContextMenu`: light grey (`#E0E0E0`) panels with a dark border and shadow, an
  icon/check column, right-aligned shortcut text and two-tone separators. Highlighted items use the
  accent gradient.
- `ToolBar`: a light grey bar with a grip and an overflow button. Buttons and toggle buttons in it are
  flat until hovered; check boxes, radio buttons, text boxes and combo boxes get tool-bar sizes.

![ContextMenu](screenshots/controls/contextmenu.png)

![ToolBar](screenshots/controls/toolbar.png)

```xml
<Menu>
    <MenuItem Header="_File">
        <MenuItem Header="_New customer" InputGestureText="Ctrl+N" />
        <Separator />
        <MenuItem Header="E_xit" />
    </MenuItem>
</Menu>

<ToolBarTray>
    <ToolBar>
        <Button ToolTip="Save"><Path ... /></Button>
        <Separator />
        <ToggleButton IsChecked="True" ToolTip="Bold"><TextBlock FontWeight="Bold" Text="B" /></ToggleButton>
    </ToolBar>
</ToolBarTray>
```

![Menu, dark](screenshots/controls/menu-dark.png)

`JetPack.Styles.Menu.Dark` is the bar in the dark grey of `Button.Dark`, with dark drop-downs and
white text. (The whole app: [Light and dark](theming.md#light-and-dark).)

![ContextMenu, dark](screenshots/controls/contextmenu-dark.png)

`JetPack.Styles.ContextMenu.Dark` is a dark grey panel with white text; its submenus too. (The whole
app: [Light and dark](theming.md#light-and-dark).)

![ToolBar, dark](screenshots/controls/toolbar-dark.png)

`JetPack.Styles.ToolBar.Dark` is the dark grey of `Button.Dark` with light glyphs and labels. (The
whole app: [Light and dark](theming.md#light-and-dark).)

## StatusBar

![StatusBar](screenshots/controls/statusbar.png)

A dark grey bar with a light line along its top and small light text. Items are 16px apart and the
outer ones 10px from the edges. Dock it at the bottom of the window for the footer of the original
navigation template:

```xml
<StatusBar DockPanel.Dock="Bottom">
    <StatusBarItem Content="Ready" />
    <StatusBarItem Content="5 available · 9 assigned" />
    <StatusBarItem HorizontalAlignment="Right" Content="Last synced 09:42" />
</StatusBar>
```

Use `DockPanel.Dock="Right"` on items for more than one at the right end. In a window with
`ResizeMode="CanResizeWithGrip"` the bar gets extra right padding for the grip. See
[Status bar](window.md#status-bar) for the window footer.

## GridSplitter and Separator

![GridSplitter and Separator](screenshots/controls/gridsplitter-separator.png)

`GridSplitter` is a light grey bar with a dotted grip that runs along the bar: upright between columns,
lying down between rows. The grip follows the direction the splitter resizes, worked out as WPF does
(`ResizeDirection`, else the alignments, else the longer side), so a row splitter needs nothing extra:

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition />
        <RowDefinition Height="6" />
        <RowDefinition />
    </Grid.RowDefinitions>
    <Border />
    <GridSplitter Grid.Row="1" HorizontalAlignment="Stretch" ResizeBehavior="PreviousAndNext" />
    <Border Grid.Row="2" />
</Grid>
```

`Separator` is a two-tone line (dark over light) that reads as an engraved rule on the texture.

## ToolTip

![ToolTip](screenshots/controls/tooltip.png)

Tool tips use the accent button gradient with a light overlay, a highlighted top edge and white small
text, as in the original theme. They follow the accent.
