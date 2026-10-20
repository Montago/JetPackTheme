# CLAUDE.md

JetPack for WPF: the JetPack theme from Microsoft's Silverlight 4 Application Themes, rebuilt as a
WPF controls library for .NET 8+ in the style of MahApps.Metro. Repo: github.com/Montago/JetPackTheme
(default branch `master`).

## Rules

- **Everything in the repo is English**: code, comments, XAML strings, demo text, sample data,
  READMEs, commit messages. The user chats in Danish; replies can be Danish.
- Keep the original JetPack look: values come from the Silverlight files (`Brushes.xaml`,
  `CoreStyles.xaml`, `SDKStyles.xaml`, `ToolkitStyles.xaml`). Originals live in
  `D:\Dropbox\SL4Themes-RawAssets\JetPack`, `D:\Dropbox\SL4Themes-templates` and the old demo in
  `D:\Dropbox\Projects\Jetpack.Demo`.
- `Assets/Texture.png` and `Assets/Vignette.png` are Microsoft's files and are excluded from the MIT
  License (see `NOTICE.md`). Do not add more copied Microsoft assets without noting them there.
- Never commit screen captures that show the user's desktop.
- When a control, property or style key changes, update the matching page in `docs/` in the same
  commit and regenerate `docs/resource-keys.md`.

## Layout

```
src/JetPack.Wpf/            library (net8.0-windows)
  Styles/JetPack.xaml       the one dictionary apps merge; merges everything below
  Styles/Brushes.xaml, Fonts.xaml, Effects.xaml, Accents/{Green,Orange,Blue}.xaml
  Styles/Controls.*.xaml    one file per control group
  Themes/Generic.xaml       merges JetPack.xaml (needed: implicit styles don't reach subclasses
                            such as MainWindow : JetPackWindow)
  Controls/                 own controls (JetPackWindow, Flyout, NumericUpDown, ...)
  Helpers/                  ControlHelper / TextBoxHelper attached properties, converters
  Theming/                  ThemeManager, AccentGenerator
src/JetPack.Wpf.Demo/       demo app; pages in Views/
samples/                    sample apps: JetPack.Wpf.Samples.Admin (HamburgerMenu) and .Portal
                            (navigation bar); presentation only, fixed data in SampleData.cs, one
                            UserControl per page in Views/, Shared/WindowSnapshot.cs for --snapshot
docs/                       component documentation (Markdown); keep it in step with the code
docs/resource-keys.md       generated: run `python tools/generate_resource_keys.py` after changing Styles/
design/jetpack-preview.src.html   approved HTML/CSS draft (texture as __TEX__ placeholder)
design/jetpack-preview.html       same file with the texture inlined as base64
docs/screenshots/           README images, produced by --snapshot
docs/screenshots/controls/  one picture per control for the docs pages (from Views/GalleryPage.xaml)
```

Resource keys follow MahApps with a `JetPack.` prefix (`JetPack.Brushes.Accent`,
`JetPack.Styles.Button.Accent`, ...). ThemeManager swaps accents by adding dictionaries at the end of
`Application.Resources`, so brushes must be referenced with `DynamicResource`.

## Build and verify

```bash
dotnet build JetPack.Wpf.sln
src/JetPack.Wpf.Demo/bin/Debug/net8.0-windows/JetPack.Wpf.Demo.exe --snapshot <folder>
```

- `--snapshot` renders every demo page (plus orange/blue accent, light title bar, dialog, flyout) to
  PNG via RenderTargetBitmap and exits; it forces en-US so screenshots stay English. Look at the
  PNGs after every visual change, then copy the relevant ones to `docs/screenshots`.
- It also renders each sample of `Views/GalleryPage.xaml` (a `Demo.Sample` whose `Tag` is the file
  name) to `<folder>/controls`; copy those to `docs/screenshots/controls`. Elements marked
  `d:Demo.Open="True"` get their drop-down, menu, tool tip or validation call-out opened, and
  `GallerySnapshot` draws the popup where its placement would put it. A new control or variant gets a
  sample there and a picture under its heading in `docs/`.
- The sample apps also take `--snapshot <folder>` (one PNG per page, named `admin-*` / `portal-*`);
  copy them to `docs/screenshots/samples` when the apps or the look change.
- RenderTargetBitmap returns blank (fully transparent) images while the PC's screen is off or the
  session is locked; connect (e.g. RDP) before running `--snapshot`.
- The user often runs the demo from Visual Studio, which locks `bin\...\JetPack.Wpf.dll`. Do not kill
  it; build to another folder instead: `dotnet build src/JetPack.Wpf.Demo -o <scratch>/out`.
- RenderTargetBitmap does not show what DWM draws (rounded window corners, system frame) or popups
  (drop-downs, menus, tooltips). For those, start the exe on screen, drive it with UI Automation from
  PowerShell (SelectionItemPattern on named elements) and capture with `Graphics.CopyFromScreen`
  after `SetProcessDPIAware`.
- After editing XAML, check that every `{StaticResource X}` is defined earlier in the same file
  (a small script over `Styles/*.xaml` did this).

## NuGet package and releases

- Package metadata lives in `src/JetPack.Wpf/JetPack.Wpf.csproj`; `PACKAGE.md` becomes the package
  README on nuget.org (absolute image URLs only), `icon.png` the package icon. The package uses a
  license file, not an MIT expression, because of the two Microsoft images.
- `.github/workflows/release.yml` publishes on a `v*.*.*` tag: always to GitHub Packages (workflow
  token, `--no-symbols` because that feed rejects snupkg), and to nuget.org with Trusted Publishing
  (`NuGet/login@v1`, OIDC, policy for `release.yml`) when the `NUGET_USER` secret exists. The version
  comes from the tag. Never handle API keys or secrets yourself; the user sets them.
- `dotnet nuget push artifacts/*.nupkg` does not expand the glob on the Windows runner; loop in bash.
- `gh api` here lacks `read:packages`; check packages on
  https://github.com/Montago/JetPackTheme/pkgs/nuget/JetPack.Wpf instead.
- To test the package without touching the global NuGet cache: pack with `-p:Version=0.0.1-localtest`
  into a scratch feed, then build a small consumer app with its own `nuget.config` and
  `--packages <scratch>/pkgs`.

## WPF gotchas found in this project

- **StaticResource / BasedOn across files fails** at runtime. Each `Controls.*.xaml` is loaded on its
  own, so cross-file references must be `DynamicResource`, and `BasedOn` styles must live in the same
  file as their base (that is why the ToolBar child style keys sit in Buttons/Toggles/Text/Lists).
- **Built-in theme styles still apply under implicit styles.** Aero2 sets e.g. `Foreground` on
  TabItem, TreeViewItem and Button, which overrides inheritance (black text on dark backgrounds).
  Fix with `OverridesDefaultStyle=True` on styles that supply a full template (done for TabItem,
  TreeViewItem, window caption/command buttons).
- **TabItem properties leak into the page.** Page content inherits FontSize/FontWeight/Foreground
  from the TabItem, and TabControl aligns the content by the TabItem's `HorizontalContentAlignment`.
  Keep header fonts/colours on the header ContentPresenter (`ControlHelper.HeaderForeground`) and
  set `HorizontalContentAlignment=Stretch` on tab items.
- **Text rendering:** the window uses `TextFormattingMode=Display` (crisp small text); headings and
  tab headers use `Ideal`, otherwise large text looks jagged.
- **Culture set inside async `OnStartup` is reset at the first await.** Set thread culture in the
  `App` constructor. WPF also formats bindings as en-US unless `FrameworkElement.LanguageProperty`
  is overridden.
- **Rounded corners** come from `DwmSetWindowAttribute` (corner preference 33, border colour 34) on
  Windows 11; on Windows 10 they stay square. The template drops its own border when
  `HasSystemRoundedCorners` is true.
- `Checked` handlers on RadioButtons with `IsChecked="True"` fire during `InitializeComponent`,
  before later attributes such as `Tag` are set; guard with `IsLoaded`.
- **Submenu items inherit Foreground from their header MenuItem through the logical tree**, not from
  the popup they sit in. Header templates therefore set `TextElement.Foreground` on the header
  presenter, never `Foreground` on the MenuItem itself.
- A Binding inside a ControlTemplate cannot be removed with `BindingOperations.ClearBinding` (that
  only clears local values); setting the property routes through a two-way template binding. Put a
  local binding on top instead (see `GallerySnapshot.ForceOpen`).
- Effects (DropShadowEffect) on text turn off ClearType; put shadows on a separate background
  Border rather than on content, except for the deliberate text emboss/glow.

## Known limitations / ideas

- TabControl supports top tabs only; DatePicker watermark text comes from WPF and is English.
- Popups (combo drop-downs, menus, tooltips, DatePicker) are checked in the gallery snapshots, but
  not yet driven interactively with the mouse.
- The HTML draft in `design/` could be turned into a reusable CSS theme (`jetpack.css` styling
  native elements, small JS for drop-downs/tabs).
