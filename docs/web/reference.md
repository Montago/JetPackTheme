---
title: Reference
parent: JetPack for the web
nav_order: 5
---

# Reference

## Tag helpers

Every tag helper in `JetPack.Web.TagHelpers`, with its attributes. The control pages have the
details and the HTML each one writes. The tag helpers marked * also take `is-enabled`, `tool-tip` and
`validation-error`.

| Tag helper | WPF | Attributes | Page |
| --- | --- | --- | --- |
| `jp-button` * | `Button` | `button-style`, `content`, `type`, `href` | [Buttons](buttons.md#button) |
| `jp-toggle-button` * | `ToggleButton` | as `jp-button`, `is-checked` | [Buttons](buttons.md#togglebutton-and-repeatbutton) |
| `jp-repeat-button` * | `RepeatButton` | as `jp-button` | [Buttons](buttons.md#togglebutton-and-repeatbutton) |
| `jp-hyperlink` * | `Hyperlink` | `href` | [Buttons](buttons.md#hyperlink) |
| `jp-split-button` * | `jp:SplitButton` | `content`, `button-style`, `id`; children: `jp-menu-item` | [Buttons](buttons.md#splitbutton) |
| `jp-drop-down-button` * | `jp:DropDownButton` | as `jp-split-button` | [Buttons](buttons.md#dropdownbutton) |
| `jp-tile` | `jp:Tile` | `title`, `count`, `tile-style`, `href` | [Buttons](buttons.md#tile) |
| `jp-badged` * | `jp:Badged` | `badge` | [Buttons](buttons.md#badged) |
| `jp-text-block` * | `TextBlock` | `text-block-style`, `text` | [Typography](typography.md#text-styles) |
| `jp-icon` | `jp:JetPackIcon` | `kind`, `label` | [Typography](typography.md#icons) |
| `jp-label` | `Label` | `content`, `target` | [Text and input](inputs.md#label) |
| `jp-text-box` * | `TextBox` | `asp-for`, `text`, `name`, `id`, `type`, `text-box-style`, `watermark`, `clear-text-button`, `is-read-only` | [Text and input](inputs.md#textbox) |
| `jp-password-box` * | `PasswordBox` | as `jp-text-box`, `password` | [Text and input](inputs.md#passwordbox) |
| `jp-combo-box` * | `ComboBox` | `asp-for`, `asp-items`, `selected-index`, `is-editable`, `text`, `watermark`, `name`, `id`; children: `jp-combo-box-item` (`content`, `value`, `is-enabled`) | [Text and input](inputs.md#combobox) |
| `jp-numeric-up-down` * | `jp:NumericUpDown` | `asp-for`, `value`, `name`, `minimum`, `maximum`, `interval`, `string-format`, `watermark`, `dark` | [Text and input](inputs.md#numericupdown) |
| `jp-auto-suggest-box` * | `jp:AutoSuggestBox` | `items`, `text`, `watermark`, `name` | [Text and input](inputs.md#autosuggestbox) |
| `jp-search-combo-box` * | `jp:SearchComboBox` | `items`, `display-member-path`, `selected-value-path`, `selected-value`, `selected-item`, `filter-mode`, `watermark`, `name`, `id` | [Text and input](inputs.md#searchcombobox) |
| `jp-date-picker` * | `DatePicker` | `selected-date`, `watermark`, `name` | [Text and input](inputs.md#datepicker) |
| `jp-time-picker` * | `jp:TimePicker` | `selected-time`, `minute-step`, `watermark`, `name` | [Text and input](inputs.md#timepicker) |
| `jp-color-picker` * | `jp:ColorPicker` | `selected-color`, `id` | [Text and input](inputs.md#colorpicker) |
| `jp-check-box` * | `CheckBox` | `asp-for`, `content`, `is-checked`, `name`, `value`, `id`, `on-light` | [Selection](selection.md#checkbox-and-radiobutton) |
| `jp-radio-button` * | `RadioButton` | as `jp-check-box`, `group-name` | [Selection](selection.md#checkbox-and-radiobutton) |
| `jp-toggle-switch` * | `jp:ToggleSwitch` | `asp-for`, `content`, `is-checked`, `name`, `id` | [Selection](selection.md#toggleswitch) |
| `jp-slider` * | `Slider` | `value`, `minimum`, `maximum`, `small-change`, `large-change`, `orientation`, `tick-placement`, `tick-frequency`, `is-snap-to-tick-enabled`, `name`, `label` | [Selection](selection.md#slider) |
| `jp-range-slider` * | `jp:RangeSlider` | as `jp-slider`, `lower-value`, `upper-value` | [Selection](selection.md#rangeslider) |
| `jp-rating` * | `jp:Rating` | `value`, `maximum`, `is-read-only` | [Selection](selection.md#rating) |
| `jp-list-box` * | `ListBox` | `selected-index`, `name`, `dark`; children: `jp-list-box-item` (`content`, `value`, `is-enabled`) | [Lists and data](lists.md#listbox) |
| `jp-list-view` * | `ListView` + `GridView` | as `jp-data-grid`; children: `jp-grid-view-column` (`display-member-binding` …) | [Lists and data](lists.md#listview-with-gridview) |
| `jp-tree-view` * | `TreeView` | `name`; children: `jp-tree-view-item` (`header`, `is-expanded`, `is-selected`, `value`) | [Lists and data](lists.md#treeview) |
| `jp-data-grid` * | `DataGrid` | `items`, `selected-index`, `height`, `selection-mode`; children: `jp-data-grid-text-column` (`header`, `binding`, `width`, `string-format`, `horizontal-alignment`, `sort-direction`) | [Lists and data](lists.md#datagrid) |
| `jp-calendar` * | `Calendar` | `selected-date`, `display-date` | [Lists and data](lists.md#calendar) |
| `jp-scroll-viewer` * | `ScrollViewer` | `horizontal-scroll-bar-visibility`, `vertical-scroll-bar-visibility`, `thin` | [Lists and data](lists.md#scrollviewer-and-scrollbar) |
| `jp-tab-control` * | `TabControl` | `tab-control-style`, `selected-index`, `selection-animation`, `content-transition`, `use-hash`, `id`; children: `jp-tab-item` (`header`, `name`, `is-selected`, `is-enabled`), `jp-tab-control-tag` | [Navigation](navigation.md#tabcontrol) |
| `jp-hamburger-menu` * | `jp:HamburgerMenu` | `selected-index`, `is-pane-open`, `pane-width`; children: `jp-hamburger-menu-item` (`content`, `icon`, `href`, `is-selected`) | [Navigation](navigation.md#hamburgermenu) |
| `jp-expander` * | `Expander` | `header`, `is-expanded` | [Navigation](navigation.md#expander) |
| `jp-accordion` * | `jp:Accordion` | children: `jp-expander` | [Navigation](navigation.md#accordion) |
| `jp-group-box` | `GroupBox` | `header` | [Navigation](navigation.md#groupbox) |
| `jp-menu` * | `Menu` | children: `jp-menu-item` | [Navigation](navigation.md#menu-contextmenu-and-toolbar) |
| `jp-menu-item` * | `MenuItem` | `header`, `input-gesture-text`, `icon`, `is-checkable`, `is-checked`, `href`; children: `jp-menu-item`, `jp-separator` | [Navigation](navigation.md#menu-contextmenu-and-toolbar) |
| `jp-context-menu` | `ContextMenu` | `id`; children: `jp-menu-item` | [Navigation](navigation.md#contextmenu) |
| `jp-tool-bar-tray`, `jp-tool-bar` | `ToolBarTray`, `ToolBar` | | [Navigation](navigation.md#toolbar) |
| `jp-status-bar` | `StatusBar` | children: `jp-status-bar-item` (`content`, `horizontal-alignment`) | [Navigation](navigation.md#statusbar) |
| `jp-grid-splitter` * | `GridSplitter` | | [Navigation](navigation.md#gridsplitter-and-separator) |
| `jp-separator` | `Separator` | | [Navigation](navigation.md#gridsplitter-and-separator) |
| `jp-tool-tip` (on any element) | `ToolTip` | the text | [Navigation](navigation.md#tooltip) |
| `jp-flyout` * | `jp:Flyout` | `id`, `header`, `position`, `width`, `is-open`, `is-closable`, `is-auto-close-enabled`, `auto-close-interval`, `padding` | [Window](window.md#flyouts) |
| `jp-progress-bar` * | `ProgressBar` | `value`, `maximum`, `is-indeterminate` | [Status](status.md#progressbar) |
| `jp-progress-ring` * | `jp:ProgressRing` | `is-active` | [Status](status.md#progressring) |
| `jp-busy-indicator` * | `jp:BusyIndicator` | `is-busy`, `busy-content` | [Status](status.md#busyindicator) |
| `jp-info-bar` | `jp:InfoBar` | `severity`, `title`, `message`, `is-closable`, `is-open` | [Status](status.md#infobar) |

## CSS classes

Class names follow the WPF controls and style keys, so a page reads like its XAML:

| WPF | HTML |
| --- | --- |
| control `ComboBox` | `.jp-combo-box` |
| style key `JetPack.Styles.Button.Accent` | `.jp-button.jp-button--accent` |
| part of a control (the count of a `Tile`) | `.jp-tile__count` |
| state (an open flyout, a folded pane) | `.is-open`, `.is-collapsed` |
| resource key `JetPack.Brushes.Control.Background` | `var(--jp-control-background)` |
| resource key `JetPack.Colors.Accent.TextShadow` | `var(--jp-color-accent-text-shadow)` |

Where HTML has a native element, the class goes on that element: `<button class="jp-button">`,
`<input class="jp-text-box">`, `<select class="jp-combo-box">`, `<progress class="jp-progress-bar">`,
`<dialog class="jp-dialog">`, `<details class="jp-expander">`, `<hr class="jp-separator">`. The
comments at the top of each section of `jetpack.css` show the markup of the controls that need more.

All rules sit in cascade layers (`jetpack-base` for the page and the headings, `components` for the
controls), so any rule outside a layer, and Tailwind's `utilities` layer, wins over them. State comes
from ARIA attributes where there is one (`aria-selected`, `aria-pressed`, `aria-checked`,
`aria-invalid`, `aria-current="page"`), so script and screen readers see the same state.

## Data attributes

`jetpack.js` works from classes and these attributes; they are set by the tag helpers, and you can
write them in your own markup:

| Attribute | On | Does |
| --- | --- | --- |
| `data-jp-open="id"` | any element | opens (or closes) the flyout or `<dialog>` with that id |
| `data-jp-toggle-open="false"` | with `data-jp-open` | only opens, never closes |
| `data-jp-close` | an element in a flyout or dialog | closes it |
| `data-jp-auto-close="ms"` | `.jp-flyout` | closes it after that many milliseconds |
| `data-jp-dismiss` | a button in `.jp-info-bar` | hides the bar |
| `data-jp-clear` | a button next to a text box | empties the box (`ClearTextButton`) |
| `data-jp-step="1"` / `"-1"` | a button in `.jp-numeric-up-down` | steps the number |
| `data-jp-toggle-pane` | a button in `.jp-hamburger-menu` | folds or opens the pane |
| `data-jp-pane-switch` | a check box | folds or opens the window's pane |
| `data-jp-window-pane` | `.jp-hamburger-menu` | a window pane outside `.jp-window__body` |
| `data-jp-accent` | a radio button | switches to its `value` when checked |
| `data-jp-accent-button="green"` | a button | switches the accent when clicked |
| `data-jp-focus-visuals` | a check box | turns the focus ring on and off |
| `data-jp-tool-tip="text"` | any element | the JetPack tool tip |
| `aria-invalid="true"` + `data-jp-error="message"` | a field | the error look and the red call-out |
| `data-jp-context-menu="id"` | any element | opens that `.jp-context-menu` on a right-click |
| `data-jp-align="right"` | `.jp-context-menu` | lines a drop-down menu up with its button's right edge |
| `data-jp-repeat` | a button | `RepeatButton`: clicks again while held |
| `data-jp-toggle` | a button | `ToggleButton`: flips `aria-pressed` |
| `data-jp-palette="#… #…"` | `.jp-color-picker` | the colours of the palette |
| `data-href="url"` | a `<tr>` | the row opens that page when clicked |
| `data-jp-hash="true"` | `.jp-tab-control` | keeps the selected tab in the address |
| `data-busy="true"` | `.jp-busy-indicator` | busy |
| `data-title-bar="light"` / `"texture"` | `.jp-window` | the title bar mode |
| `data-accent="orange"` | `<html>` or any element | the accent |

## Script API

`jetpack.js` sets up the controls on `DOMContentLoaded` and offers `window.JetPack`:

| Member | Description |
| --- | --- |
| `JetPack.ThemeManager.changeAccent(nameOrColor)` | `'green'`, `'orange'`, `'blue'` or a `#RRGGBB` colour; remembered in the browser |
| `JetPack.ThemeManager.currentAccent` | the accent name, or the colour of a generated accent |
| `JetPack.ThemeManager.accents` | `['green', 'orange', 'blue']` |
| `JetPack.ThemeManager.onAccentChanged(fn)` | calls `fn(accent)` after each change |
| `JetPack.ThemeManager.controlFontSize` | get or set the control font size in px; remembered |
| `JetPack.ThemeManager.showFocusVisuals` | get or set the focus ring; remembered |
| `JetPack.showDialog(options)` | a modal dialog; a promise of `'affirmative'` or `'negative'` (see [Dialogs](window.md#dialogs)) |
| `JetPack.showMessage(title, message)` | a dialog with one OK button |
| `JetPack.openFlyout(idOrElement)`, `JetPack.closeFlyout(idOrElement)` | open and close a flyout (or a `<dialog>`) |
| `JetPack.init(element)` | sets up the controls inside `element` that a script added later |
| `JetPack.formatDate(date)`, `JetPack.parseDate(text)` | dates in the pickers' format (M/d/yyyy) |
| `element.jetpack.select(indexOrName)`, `element.jetpack.selectedIndex` | on a `.jp-tab-control` |

The stored choices are kept in `localStorage` under `jetpack.accent`, `jetpack.focusVisuals`,
`jetpack.controlFontSize` and `jetpack.paneOpen`.

## Events

Controls raise the usual `input` and `change` events of their inputs. Value controls also raise
`jp-change`, which bubbles, with the new value in `event.detail`:

| Control | Raised on | `event.detail` |
| --- | --- | --- |
| `jp-combo-box` | the `<select>` | the selected option's value |
| editable `jp-combo-box` | `.jp-combo-box--editable` | the text |
| `jp-list-box`, `jp-list-view`, `jp-data-grid`, `jp-tree-view` | the list | the selected element (item, `<tr>`, tree item) |
| `jp-slider` | `.jp-slider` | the value |
| `jp-range-slider` | `.jp-range-slider` | `{ lowerValue, upperValue }` |
| `jp-rating` | `.jp-rating` | the value |
| `jp-calendar` | `.jp-calendar` | a `Date` |
| `jp-date-picker` | `.jp-date-picker` | a `Date`, or `null` |
| `jp-time-picker` | `.jp-time-picker` | the time as text (`"08:30"`) |
| `jp-search-combo-box` | `.jp-search-combo-box` | `{ selectedItem, selectedValue }` |
| `jp-auto-suggest-box` | `.jp-auto-suggest-box` | the chosen text |
| `jp-color-picker` | the button | the colour (`change` too) |
| `jp-tab-control` | `.jp-tab-control` | `{ index, header, name }` |
| `jp-toggle-button` | the button | `true` or `false` |

Other events:

| Event | Raised on | When |
| --- | --- | --- |
| `jp-open`, `jp-close` | `.jp-flyout` | the flyout opens or closes |
| `jp-close` | `.jp-info-bar` | the bar is closed |
| `jp-accent-changed` | `document` | the accent changes; `event.detail` is the accent |

```js
document.addEventListener('jp-change', e => {
    if (e.target.matches('#Week')) load(e.detail);
});
```

## Generated files

`jetpack.tokens.css`, `img/texture.png`, `img/icons.svg` and `jetpack.css` are generated; do not edit
them. In the repository:

| After changing | Run |
| --- | --- |
| `Styles/Brushes.xaml`, `Fonts.xaml`, `Effects.xaml` or an accent of JetPack.Wpf | `python tools/generate_web_tokens.py`, then `python tools/bundle_web_css.py` |
| `jetpack.src.css` or `css/controls/*.css` | `python tools/bundle_web_css.py` |
| `icons/icons.json` | `python tools/generate_icons.py --fetch` |

`python tools/update_docs.py --check` reports generated files that are out of date, and CI fails
when `jetpack.css` does not match its sources.
