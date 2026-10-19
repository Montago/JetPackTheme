# Notice

The source code in this repository is licensed under the MIT License (see `LICENSE`),
**except for the two files listed below**.

## Files copied from Microsoft (not covered by the MIT License)

| File | Original file |
| --- | --- |
| `src/JetPack.Wpf/Assets/Texture.png` | `JetPack/Application/Images/backgroundtexture.png` |
| `src/JetPack.Wpf/Assets/Vignette.png` | `JetPack/Application/Images/backgroundshadow.png` |

Both files are copied unchanged from the JetPack theme in **Silverlight 4 Application Themes**
(`SL4Themes-templates.zip`), published by Microsoft on the Microsoft Download Center:
https://www.microsoft.com/en-us/download/details.aspx?id=14590

Copyright © Microsoft Corporation. The download does not state a license; Microsoft describes
the themes as intended "to use in your own application". The files are included here so the
theme looks like the original. If you need to be sure of the rights, replace them with your own
images of the same size (a tiled 200×200 texture and a vignette with transparency).

## Other origins

**Visual design.** Colours, gradient values and shadow settings are taken from the JetPack
theme's `Brushes.xaml` and `CoreStyles.xaml` in the same download. The WPF styles and
templates in this repository are written from scratch; no XAML is copied from the download.

**API shape.** Resource key naming, `ThemeManager`, `TextBoxHelper` and the choice of extra
controls follow MahApps.Metro (MIT License, https://github.com/MahApps/MahApps.Metro). This
repository contains no source code copied from MahApps.Metro.
