# JetPack for the web

The JetPack look of [JetPack.Wpf](https://www.nuget.org/packages/JetPack.Wpf) in HTML and CSS, for
admin portals and other business sites on ASP.NET Core. The package is a Razor class library with:

- **`jetpack.css`**: every control of the WPF library as CSS classes named after the WPF controls and
  style keys (`.jp-combo-box`, `.jp-button--accent`). The rules sit in cascade layers, so Tailwind or
  your own utility classes still win.
- **`jetpack.js`**: drop-downs, calendar, date and time pickers, sliders, menus, tabs, flyouts,
  dialogs, tool tips, validation call-outs, accents and the vignette. It has no dependencies.
- **Tag helpers** for every control: `<jp-combo-box>`, `<jp-tab-control>`, `<jp-date-picker>`,
  `<jp-data-grid>` … Their attributes are the WPF property names in kebab case.
- **JetPackGrid** (`<jp-jetpack-grid>`): the data grid of JetPack.Wpf, with a word search that marks
  its hits, a value filter in every header, three-state sorting, grouping and paging, in the browser
  or, in server mode, on the server (`JetPackGridServer.Query`).
- **Icons**: Material Design Icons as an SVG sprite, with the same names as `JetPackIcon` in WPF.

![The JetPack web demo](https://raw.githubusercontent.com/Montago/JetPackTheme/master/docs/screenshots/web/core.png)

## Getting started

```bash
dotnet add package JetPack.Web
```

Add the tag helpers in `_ViewImports.cshtml`:

```cshtml
@addTagHelper *, JetPack.Web
@using JetPack.Web.TagHelpers
```

Load the theme in the layout's `<head>`, before Tailwind if you use it. Leave `defer` off the script,
because it sets the stored accent before the page is drawn:

```html
<link rel="stylesheet" href="~/_content/JetPack.Web/css/jetpack.css" asp-append-version="true" />
<script src="~/_content/JetPack.Web/js/jetpack.js" asp-append-version="true"></script>
```

Then write the controls as you would in XAML:

```cshtml
<jp-button button-style="Accent">Save</jp-button>
<jp-text-box asp-for="Name" watermark="Search…" clear-text-button="true" />
<jp-tab-control tab-control-style="Strip">
    <jp-tab-item header="Core">…</jp-tab-item>
    <jp-tab-item header="SDK">…</jp-tab-item>
</jp-tab-control>
```

Switch the accent from script:

```js
JetPack.ThemeManager.changeAccent('orange');   // green, orange, blue or any #hex colour
```

## Documentation

Tag helpers, classes, icons and ThemeManager:
**https://montago.github.io/JetPackTheme/web.html**

Source, the web demo and issues: https://github.com/Montago/JetPackTheme

## License

MIT, except the icons (`img/icons.svg`), which are Material Design Icons under the Apache License 2.0
(`icons/` in the package). See `NOTICE.md` in the package for where the look comes from.
