---
title: Controls
parent: JetPack for the web
nav_order: 4
has_children: true
---

# Controls

Every control of the WPF library has a CSS class, a tag helper with the WPF name and, where it needs
one, behaviour in `jetpack.js`. Native elements get the look from their class (`<button class="jp-button">`,
`<select class="jp-combo-box">`, `<details class="jp-expander">`), so they still work without script.

| Page | Controls |
| --- | --- |
| [Window and dialogs](window.md) | the window frame, title bar modes, window commands, vignette, `jp-flyout`, dialogs |
| [Buttons](buttons.md) | `jp-button` and its variants, `jp-toggle-button`, `jp-repeat-button`, `jp-hyperlink`, `jp-split-button`, `jp-drop-down-button`, `jp-tile`, `jp-badged` |
| [Text and input](inputs.md) | `jp-text-box`, `jp-password-box`, `jp-label`, validation, `jp-combo-box`, `jp-numeric-up-down`, `jp-auto-suggest-box`, `jp-search-combo-box`, `jp-date-picker`, `jp-time-picker`, `jp-color-picker` |
| [Selection](selection.md) | `jp-check-box`, `jp-radio-button`, `jp-toggle-switch`, `jp-slider`, `jp-range-slider`, `jp-rating` |
| [Lists and data](lists.md) | `jp-list-box`, `jp-list-view`, `jp-tree-view`, `jp-data-grid`, `jp-jetpack-grid`, `jp-calendar`, `jp-scroll-viewer` |
| [Navigation and layout](navigation.md) | `jp-tab-control` (three looks), `jp-hamburger-menu`, `jp-expander`, `jp-accordion`, `jp-group-box`, `jp-menu`, `jp-context-menu`, `jp-tool-bar`, `jp-status-bar`, `jp-grid-splitter`, `jp-separator`, tool tips |
| [Progress and status](status.md) | `jp-progress-bar`, `jp-progress-ring`, `jp-busy-indicator`, `jp-info-bar` |

## On every page

- Each section has a picture of the control, the tag helper, the HTML it writes (for pages without
  Razor) and a table of its attributes. The picture comes from the web demo's gallery page
  (`/Demo/Gallery`), with drop-downs and menus open where the control has one.
- Attributes are the WPF property names in kebab case; enum values are the WPF names
  (`button-style="Accent"`, `orientation="Vertical"`).
- Every control tag helper takes `is-enabled="false"`, `tool-tip="…"` and `validation-error="…"`,
  so the tables leave them out. A few tag helpers that only arrange other elements
  (`jp-tile`, `jp-info-bar`, `jp-group-box`, `jp-status-bar`, `jp-tool-bar`, `jp-context-menu`) take none of them.
- `class`, `style`, `id` and other plain attributes you write on a tag helper stay on the element it
  writes (for text boxes and combo boxes: on the `<input>` or `<select>`).
- Value controls raise `jp-change` with the new value in `event.detail`; the [reference](reference.md#events)
  lists the detail of each control.

The [WPF pages](../controls.md) describe the same controls for the desktop.
