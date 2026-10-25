# Notice

Everything in this repository is licensed under the MIT License (see `LICENSE`), except the icons.

**Icons.** The SVG files in `icons/mdi/`, and the path data generated from them
(`Controls/JetPackIconKind.g.cs` in JetPack.Wpf, `wwwroot/img/icons.svg` in JetPack.Web), are
**Material Design Icons** by Pictogrammers (https://pictogrammers.com/library/mdi/, package
`@mdi/svg` 7.4.47), licensed under the Apache License 2.0. See `icons/LICENSE` (Pictogrammers Free
License) and `icons/LICENSE-2.0.txt`. The icons are used unchanged.

## Origins

**Visual design.** JetPack for WPF recreates the JetPack theme from Microsoft's
**Silverlight 4 Application Themes** (https://www.microsoft.com/en-us/download/details.aspx?id=14590).
Colours, gradient values and shadow settings are taken from the theme's `Brushes.xaml` and
`CoreStyles.xaml`. The WPF styles and templates are written from scratch; no XAML files and no image
files are copied from the download. The one value taken over as drawn is the check mark of the
theme's check box (`JetPack.Geometry.Check`, also used in `jetpack.css`): six points of path data,
kept because the mark belongs to the look. The window background (`Assets/Texture.png`) is drawn by
`tools/generate_backgrounds.py`, and the vignette is drawn in code (`Controls/Vignette.cs`).

**API shape.** Resource key naming, `ThemeManager`, `TextBoxHelper` and the choice of extra
controls follow MahApps.Metro (MIT License, https://github.com/MahApps/MahApps.Metro). This
repository contains no source code copied from MahApps.Metro.
