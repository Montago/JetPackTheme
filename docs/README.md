---
title: Home
nav_order: 1
permalink: /
---

# JetPack documentation

JetPack is the JetPack theme from Microsoft's Silverlight 4 Application Themes, rebuilt for today's
.NET: the textured grey ground, the glossy green, orange and blue accents and the embossed controls.
It comes as two packages that share one set of colours, fonts and icons, so a desktop app and a web
portal can look the same.

| | [JetPack for WPF](wpf.md) | [JetPack for the web](web.md) |
| --- | --- | --- |
| Package | [JetPack.Wpf](https://www.nuget.org/packages/JetPack.Wpf) | [JetPack.Web](https://www.nuget.org/packages/JetPack.Web) |
| For | WPF desktop apps on .NET 8 and later | ASP.NET Core sites on .NET 8 and later; the CSS and JavaScript work on any site |
| What you get | Styles for every standard WPF control, plus `JetPackWindow`, dialogs, `Flyout`, `HamburgerMenu`, `JetPackGrid` and more | CSS classes and a script for the same controls, tag helpers with the WPF names (`<jp-combo-box>`), and an SVG icon sprite |
| Start with | [Getting started with WPF](getting-started.md) | [Getting started on the web](web/getting-started.md) |

![JetPack for WPF](screenshots/core.png)

![JetPack for the web](screenshots/web/core.png)

## What both share

- **Colours and accents.** The web theme's CSS variables are generated from the WPF brushes, so
  `JetPack.Brushes.Accent` in XAML and `var(--jp-accent)` in CSS are the same colour. Both switch
  between green, orange and blue, or build an accent from any colour.
- **Names.** A control has the same name in both: `ComboBox` in XAML, `<jp-combo-box>` as a tag
  helper and `.jp-combo-box` as a CSS class. Style keys become modifiers:
  `JetPack.Styles.Button.Accent` is `button-style="Accent"` and `.jp-button--accent`.
- **Icons.** `jp:JetPackIcon` in WPF and `<jp-icon>` on the web draw the same Material Design Icons
  by the same names (`ViewGrid`, `AccountMultiple`).

## License

MIT, except the icons, which are Material Design Icons under the Apache License 2.0. See
[NOTICE](https://github.com/Montago/JetPackTheme/blob/master/NOTICE.md) for where the look comes from.
