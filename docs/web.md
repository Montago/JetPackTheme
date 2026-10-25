---
title: JetPack for the web
nav_order: 8
---

# JetPack for the web

![The Core tab of the web demo](screenshots/web/core.png)

`src/JetPack.Web` is the JetPack look in HTML and CSS, for admin portals and other business sites.
It is a Razor class library, so an ASP.NET Core app gets the files by referencing the project, and
it has a tag helper for every control of the WPF demo, named after the control
(`<jp-combo-box>`, `<jp-tab-control>`, `<jp-date-picker>` …). The CSS and JavaScript do not depend
on .NET: any site can link them and write the markup itself.

| Part | File | Contents |
| --- | --- | --- |
| Tokens | `wwwroot/css/jetpack.tokens.css` | The colours, gradients, fonts and shadows of `JetPack.Wpf` as CSS custom properties, generated from `Styles/*.xaml` |
| Components | `wwwroot/css/jetpack.css` and `css/controls/*.css` | The controls as CSS classes, one file per control group as `Controls.*.xaml`, in cascade layers |
| Behaviour | `wwwroot/js/jetpack.js` | Drop-downs, calendar, sliders, menus, tabs, flyouts, dialogs, tool tips, validation call-outs, accents and the vignette; no dependencies |
| Tag helpers | `TagHelpers/*.cs` | `<jp-*>` elements with the WPF property names |
| Icons | `wwwroot/img/icons.svg` | The JetPackIcon set as an SVG sprite |

Tailwind (or any other utility CSS) does the layout: grids, gaps, widths and breakpoints, as the
StackPanels and Grids in XAML. The JetPack classes only give the controls their look, so the two do
not get in each other's way.

## The web demo

`src/JetPack.Web.Demo` is the WPF demo (`src/JetPack.Wpf.Demo`) as a web page: the same tabs, the
same controls in the same places, the same data (it links the WPF demo's `DemoData.cs`), and the
Settings, Recent, Update and Saved flyouts of the window. Each tab is a partial view named after the
WPF UserControl (`Views/Demo/Pages/CorePage.cshtml` ↔ `Views/CorePage.xaml`), and `wwwroot/js/demo.js`
is the code-behind. The JetPackGrid tab is left out; grids of that kind are better served by a
dedicated grid library.

```bash
dotnet run --project src/JetPack.Web.Demo
```

![SDK](screenshots/web/sdk.png)

![Toolkit](screenshots/web/toolkit.png)

![Extras](screenshots/web/extras.png)

![Window](screenshots/web/window.png)

![Palette](screenshots/web/palette.png)

![Icons](screenshots/web/icons.png)

What the browser cannot do the same way: the Window tab's corner style and resize grip, and Mica in
Settings, belong to the desktop window; they are shown disabled. Dates are shown as en-US (M/d/yyyy).

## Getting started

Reference the library and add its tag helpers in `_ViewImports.cshtml`:

```xml
<ProjectReference Include="..\..\src\JetPack.Web\JetPack.Web.csproj" />
```

```cshtml
@addTagHelper *, JetPack.Web
@using JetPack.Web.TagHelpers
```

In the layout, load JetPack before Tailwind. The first `@layer` statement in `jetpack.css` fixes the
order of the layers, so Tailwind's utilities still win over the JetPack classes:

```html
<link rel="stylesheet" href="~/_content/JetPack.Web/css/jetpack.css" />
<script src="~/_content/JetPack.Web/js/jetpack.js"></script>
```

`jetpack.js` belongs in `<head>` without `defer`: it sets the stored accent before the page is
drawn. With the Tailwind CLI, also import `jetpack.tailwind.css`, which maps the tokens to theme
variables (`text-jp-soft`, `bg-jp-panel`, `shadow-jp`, ...). The demo and the sample use the browser
build of Tailwind instead, so they need no Node build step.

## Tag helpers

Attribute names are the WPF property names in kebab case, and the enums carry the WPF values:

```cshtml
<jp-button button-style="Accent">Save</jp-button>
<jp-text-box asp-for="Name" watermark="Search…" clear-text-button="true" />
<jp-combo-box selected-index="1">
    <jp-combo-box-item content="Seattle" />
    <jp-combo-box-item content="Portland" />
</jp-combo-box>
<jp-tab-control tab-control-style="Strip">
    <jp-tab-item header="Core">…</jp-tab-item>
    <jp-tab-item header="SDK">…</jp-tab-item>
</jp-tab-control>
<jp-data-grid items="Model.Timesheets" selected-index="1" height="190">
    <jp-data-grid-text-column header="Employee" binding="Employee" sort-direction="Ascending" />
    <jp-data-grid-text-column header="Hours" binding="Hours" string-format="N1" width="60" horizontal-alignment="Right" />
</jp-data-grid>
```

Every `<jp-*>` control also takes `is-enabled`, `tool-tip` and `validation-error` (the red frame and
corner, with the message in a call-out when the control has the focus or the mouse). `jp-tool-tip`
puts the JetPack tool tip on any element.

| Group | Tag helpers |
| --- | --- |
| Buttons | `jp-button` (`button-style`: Accent, Green, Orange, Blue, Square, Flat, Link), `jp-toggle-button`, `jp-repeat-button`, `jp-hyperlink`, `jp-split-button`, `jp-drop-down-button` |
| Text and input | `jp-text-block` (`text-block-style`: Heading1–6, Content, Caption), `jp-label`, `jp-separator`, `jp-text-box` (`text-box-style`: Dark, MultiLine, MultiLineDark; `watermark`, `clear-text-button`, `is-read-only`, `asp-for`), `jp-password-box`, `jp-numeric-up-down`, `jp-combo-box` + `jp-combo-box-item` (`is-editable`, `watermark`, `asp-for`/`asp-items`), `jp-date-picker`, `jp-time-picker`, `jp-auto-suggest-box`, `jp-search-combo-box`, `jp-color-picker` |
| Selection | `jp-check-box` (`is-checked="null"` for the third state), `jp-radio-button` (`group-name`), `jp-toggle-switch`, `jp-slider`, `jp-range-slider`, `jp-rating` |
| Lists and data | `jp-list-box` + `jp-list-box-item`, `jp-list-view` + `jp-grid-view-column`, `jp-data-grid` + `jp-data-grid-text-column`, `jp-tree-view` + `jp-tree-view-item`, `jp-scroll-viewer` (`thin`), `jp-calendar` |
| Containers | `jp-group-box`, `jp-expander`, `jp-accordion`, `jp-tab-control` + `jp-tab-item` + `jp-tab-control-tag` (`tab-control-style`: Strip, Navigation; `selection-animation`, `content-transition`), `jp-grid-splitter`, `jp-busy-indicator` |
| Menus | `jp-menu`, `jp-menu-item` (`input-gesture-text`, `is-checkable`, child items for a submenu), `jp-context-menu`, `jp-tool-bar-tray` + `jp-tool-bar` |
| Window | `jp-flyout` (`position`, `is-auto-close-enabled`, `auto-close-interval`), `jp-hamburger-menu` + `jp-hamburger-menu-item`, `jp-status-bar` + `jp-status-bar-item` |
| Status | `jp-info-bar`, `jp-progress-bar`, `jp-progress-ring`, `jp-badged`, `jp-tile`, `jp-icon` |

## Classes and names

The tag helpers write plain HTML with JetPack classes, so the same look is available without
Razor. Class names follow the WPF controls and style keys, so a page reads like its XAML:

| WPF | HTML |
| --- | --- |
| control `ComboBox` | `.jp-combo-box` |
| style key `JetPack.Styles.Button.Accent` | `.jp-button.jp-button--accent` |
| part of a control (the count of a `Tile`) | `.jp-tile__count` |
| resource key `JetPack.Brushes.Control.Background` | `var(--jp-control-background)` |
| resource key `JetPack.Colors.Accent.TextShadow` | `var(--jp-color-accent-text-shadow)` |

Where HTML has a native element, the class goes on that element: `<button class="jp-button">`,
`<input class="jp-text-box">`, `<select class="jp-combo-box">`, `<progress class="jp-progress-bar">`,
`<dialog class="jp-dialog">`, `<details class="jp-expander">`. A `<select class="jp-combo-box">`
opens the JetPack drop-down list; without script it is still a working select. The other controls
(calendar, sliders, menus, flyouts …) take their markup from the tag helper; the comments at the top
of each `css/controls/*.css` section show it for hand-written pages.

Invalid fields get the error look from `aria-invalid="true"` or from ASP.NET Core's own
`input-validation-error` class, so `asp-for` and `asp-validation-for` need nothing extra.

## Icons

The same icons as `jp:JetPackIcon` in WPF, as an SVG sprite (`img/icons.svg`, one `<symbol>` per
icon, named after the MDI icon). They take the text colour and are 16×16 by default, 18×18 in `jp-hamburger-menu`:

```cshtml
<jp-icon kind="ViewGrid" />
<jp-icon kind="Bell" label="Notifications" />   @* label: for an icon that stands alone *@
```

```html
<svg class="jp-icon" aria-hidden="true"><use href="/_content/JetPack.Web/img/icons.svg#view-grid"></use></svg>
```

## Accents, ThemeManager and dialogs

The accent is a `data-accent` attribute (`green`, `orange` or `blue`) on `<html>`, or on any element
for a part of the page. From script:

```js
JetPack.ThemeManager.changeAccent('orange');   // remembered in the browser
JetPack.ThemeManager.changeAccent('#8E44AD');  // a whole accent from one colour (AccentGenerator)
JetPack.ThemeManager.controlFontSize = 11;     // ThemeManager.ControlFontSize
JetPack.ThemeManager.showFocusVisuals = false; // ThemeManager.ShowFocusVisuals

await JetPack.showMessage('Timesheet sent', 'Your timesheet has been sent for approval.');
const result = await JetPack.showDialog({ title: 'Delete company?', content: '…',
    affirmativeText: 'Delete', negativeText: 'Cancel', affirmativeStyle: 'orange' });  // 'affirmative' | 'negative'
```

Radio buttons with `data-jp-accent` and a check box with `data-jp-focus-visuals` do the same without
script; `data-jp-open="id"` opens a flyout or a dialog. The other data attributes are listed at the
top of `jetpack.js`. Value controls raise `jp-change` with the new value in `event.detail`.

## Values from JetPack.Wpf

`jetpack.tokens.css` and `img/texture.png` are generated; do not edit them. After changing
`Brushes.xaml`, `Fonts.xaml`, `Effects.xaml` or an accent, run:

```bash
python tools/generate_web_tokens.py
```

`python tools/update_docs.py --check` reports when the generated files are out of date. The
vignette is drawn by `jetpack.js` in the same way as `Controls/Vignette.cs`, dithered so it does not
band. The pictures on this page come from `python tools/web_snapshots.py` (headless Edge).

## Sample site

`samples/JetPack.Web.Samples.Mvc` is the [admin sample](samples.md) as an ASP.NET Core MVC site, with
the same pages and the same `SampleData.cs`. Filters are plain GET forms and the employee form posts
back with server-side validation; nothing is stored.

```bash
dotnet run --project samples/JetPack.Web.Samples.Mvc
```

![Employees](screenshots/samples/web-employees.png)

![Dashboard](screenshots/samples/web-dashboard.png)

![Timesheets with the approval menu](screenshots/samples/web-timesheets.png)

![Projects](screenshots/samples/web-projects.png)

## Limitations

- Not published as a package yet.
- JetPackGrid has no web version; use a grid library for large, filterable tables.
- Scroll bars get the JetPack look only in Chromium browsers (Edge, Chrome); Firefox gets colours only.
- The imported files under `css/controls/` carry no version in their address, so after an update a
  browser may keep an old one until it revalidates its cache.
- `jetpack.tailwind.css` has not been tried with the Tailwind CLI yet; the demo and the sample use the browser build.
