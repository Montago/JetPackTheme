# Notice

Everything in this repository is licensed under the MIT License (see `LICENSE`).

## Origins

**Visual design.** JetPack for WPF recreates the JetPack theme from Microsoft's
**Silverlight 4 Application Themes** (https://www.microsoft.com/en-us/download/details.aspx?id=14590).
Colours, gradient values and shadow settings are taken from the theme's `Brushes.xaml` and
`CoreStyles.xaml`. The WPF styles and templates are written from scratch; no XAML and no image
files are copied from the download. The window background (`Assets/Texture.png`) is drawn by
`tools/generate_backgrounds.py`, and the vignette is drawn in code (`Controls/Vignette.cs`).

**API shape.** Resource key naming, `ThemeManager`, `TextBoxHelper` and the choice of extra
controls follow MahApps.Metro (MIT License, https://github.com/MahApps/MahApps.Metro). This
repository contains no source code copied from MahApps.Metro.
