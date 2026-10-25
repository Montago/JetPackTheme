---
title: JetPack for the web
nav_order: 8
---

# JetPack for the web

![The admin sample as an ASP.NET Core MVC site](screenshots/samples/web-employees.png)

`src/JetPack.Web` is a proof of concept of the JetPack look in HTML and CSS, for admin portals
and other business sites. It is a Razor class library, so an ASP.NET Core app gets the files by
referencing the project, but the CSS and JavaScript themselves do not depend on .NET: any site can
link them.

It has three parts:

| Part | File | Contents |
| --- | --- | --- |
| Tokens | `wwwroot/css/jetpack.tokens.css` | The colours, gradients, fonts and shadows of `JetPack.Wpf` as CSS custom properties, generated from `Styles/*.xaml` |
| Components | `wwwroot/css/jetpack.css` | The controls as CSS classes, in cascade layers |
| Behaviour | `wwwroot/js/jetpack.js` | Accent switching, flyouts, dialogs, menus and the vignette; no dependencies |

Tailwind (or any other utility CSS) does the layout: grids, gaps, widths and breakpoints. The
JetPack classes only give the controls their look, so the two do not get in each other's way.

## Getting started

Reference the library and add its tag helpers in `_ViewImports.cshtml`:

```xml
<ProjectReference Include="..\..\src\JetPack.Web\JetPack.Web.csproj" />
```

```cshtml
@addTagHelper *, JetPack.Web
```

In the layout, load JetPack before Tailwind. The first `@layer` statement in `jetpack.css` fixes the
order of the layers, so Tailwind's utilities still win over the JetPack classes:

```html
<link rel="stylesheet" href="~/_content/JetPack.Web/css/jetpack.css" />
<script src="~/_content/JetPack.Web/js/jetpack.js"></script>
```

`jetpack.js` belongs in `<head>` without `defer`: it sets the stored accent before the page is
drawn. With the Tailwind CLI, also import `jetpack.tailwind.css`, which maps the tokens to theme
variables (`text-jp-soft`, `bg-jp-panel`, `shadow-jp`, ...). The sample uses the browser build of
Tailwind instead, so it needs no Node build step.

## Names

Class names follow the WPF controls and style keys, so a page reads like its XAML:

| WPF | HTML |
| --- | --- |
| control `ComboBox` | `.jp-combo-box` |
| style key `JetPack.Styles.Button.Accent` | `.jp-button.jp-button--accent` |
| part of a control (the count of a `Tile`) | `.jp-tile__count` |
| resource key `JetPack.Brushes.Control.Background` | `var(--jp-control-background)` |
| resource key `JetPack.Colors.Accent.TextShadow` | `var(--jp-color-accent-text-shadow)` |

Where HTML has a native element, the class goes on that element: `<button class="jp-button">`,
`<input class="jp-text-box">`, `<select class="jp-combo-box">`, `<progress class="jp-progress-bar">`,
`<dialog class="jp-dialog">`, `<details class="jp-expander">`.

| Group | Classes |
| --- | --- |
| Window | `jp-window` (body), `jp-title-bar`, `jp-window-commands`, `jp-hamburger-menu` (`__toggle`, `__item`, `__label`), `jp-status-bar`, `jp-vignette` (canvas) |
| Buttons | `jp-button` with `--accent`, `--green`, `--orange`, `--blue`, `--square`, `--flat`, `--link`; `aria-pressed` for ToggleButton; `jp-split-button`, `jp-caret`, `jp-hyperlink` |
| Text and input | `jp-text-box` (`--dark`), `jp-text-box-host` with `__clear` (ClearTextButton), `jp-numeric-up-down`, `jp-combo-box`, `jp-label`, `jp-validation-message`, `jp-validation-summary` |
| Selection | `jp-check-box`, `jp-radio-button`, `jp-toggle-switch` (on a `<label>` around the input), `jp-rating` |
| Lists and data | `jp-data-grid` (`th[aria-sort]`, `tr[aria-selected]`, `tr[data-href]`, `__number`), `jp-tree-view` (`__item`), `jp-scroll-viewer` |
| Containers | `jp-group-box` (`__header`, `__content`), `jp-expander`, `jp-tab-control__strip`, `jp-tab-item`, `jp-tab-control__content` |
| Status | `jp-info-bar` (`--success`, `--warning`, `--error`), `jp-progress-bar`, `jp-badged`, `jp-pill`, `jp-tile` (`--accent`, `--green`, `--orange`, `--blue`), `jp-tool-tip` |
| Menus and dialogs | `jp-context-menu` (a `popover`), `jp-menu-item`, `jp-menu-separator`, `jp-flyout`, `jp-dialog` |
| Typography | `h1`–`h6` or `jp-text-block--heading1` … `--heading6`, `--caption`, `--content` |
| Icons | `jp-icon` (see below) |

Invalid fields get the error look from `aria-invalid="true"` or from ASP.NET Core's own
`input-validation-error` class, so `asp-for` and `asp-validation-for` need nothing extra.

## Tag helpers

Controls that need more markup than a class have a tag helper with the WPF name:

```cshtml
<jp-info-bar severity="Warning" title="4 timesheets" message="are waiting for your approval.">
    <a class="jp-button" href="/timesheets">Review</a>
</jp-info-bar>
<jp-tile title="Active employees" count="8" tile-style="Green" href="/employees" />
<jp-group-box header="Details">…</jp-group-box>
<jp-numeric-up-down asp-for="HoursPerWeek" minimum="0" maximum="40" interval="0.5" />
<jp-toggle-switch asp-for="IsActive">Active</jp-toggle-switch>
<jp-icon kind="Bell" />
```

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

## Accents and ThemeManager

The accent is a `data-accent` attribute (`green`, `orange` or `blue`) on `<html>`, or on any element
for a part of the page. From script:

```js
JetPack.ThemeManager.changeAccent('orange');   // remembered in the browser
JetPack.ThemeManager.showFocusVisuals = false;  // ThemeManager.ShowFocusVisuals
```

Radio buttons with `data-jp-accent` and a check box with `data-jp-focus-visuals` do the same without
script. The other data attributes (`data-jp-open`, `data-jp-close`, `data-jp-dismiss`,
`data-jp-clear`, `data-jp-step`, `data-jp-toggle-pane`, `data-jp-pane-switch`) are listed at the top
of `jetpack.js`.

## Values from JetPack.Wpf

`jetpack.tokens.css` and `img/texture.png` are generated; do not edit them. After changing
`Brushes.xaml`, `Fonts.xaml`, `Effects.xaml` or an accent, run:

```bash
python tools/generate_web_tokens.py
```

`python tools/update_docs.py --check` reports when the generated files are out of date. The
vignette is drawn by `jetpack.js` in the same way as `Controls/Vignette.cs`, dithered so it does not
band.

## Sample site

`samples/JetPack.Web.Samples.Mvc` is the [admin sample](samples.md) as an ASP.NET Core MVC site, with
the same pages and the same `SampleData.cs`. Filters are plain GET forms and the employee form posts
back with server-side validation; nothing is stored.

```bash
dotnet run --project samples/JetPack.Web.Samples.Mvc
```

![Dashboard](screenshots/samples/web-dashboard.png)

![Timesheets with the approval menu](screenshots/samples/web-timesheets.png)

![Projects](screenshots/samples/web-projects.png)

## Limitations

- Proof of concept: not published as a package, and only the controls the admin sample needs.
  Calendar, date and time pickers, slider, RangeSlider, ColorPicker, AutoSuggestBox and JetPackGrid
  are still missing; dates use the browser's own `<input type="date">`.
- The drop-down list of a `<select>` is drawn by the browser, not in the JetPack look.
- Scroll bars get the JetPack look only in Chromium browsers (Edge, Chrome); Firefox gets colours only.
- `jetpack.tailwind.css` has not been tried with the Tailwind CLI yet; the sample uses the browser build.
