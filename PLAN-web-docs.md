# Plan: web documentation as thorough as the WPF docs

Branch `docs/web-and-wpf` (local, not pushed). Delete this file before the PR is merged.

## Done
- **Commit 7574e9f.** The docs nav is split into two sections:
  - `docs/README.md` is the new home page.
  - `docs/wpf.md` is the parent of the WPF pages. Their URLs are unchanged; controls pages have
    `parent: Controls` + `grand_parent: JetPack for WPF`.
  - `docs/web.md` is the parent of the web pages (`has_children: true`, `nav_order: 3`).
  - `_config.yml` title is "JetPack".
- **Uncommitted, untested:**
  - `src/JetPack.Web.Demo/Views/Demo/Gallery.cshtml`: one `.gallery-sample[data-shot=name]` per
    control, with the same names as the WPF `GalleryPage.xaml`. JetPackGrid and resizegrip are left
    out; `window`, `window-light` and `dialog` are added.
  - `wwwroot/css/gallery.css` and `wwwroot/js/gallery.js` in the web demo.
  - `DemoController.Gallery()`.
  - `tools/web_snapshots.py` takes `Demo/Gallery?shot=<name>` and cuts away the magenta, giving
    `docs/screenshots/web/controls/<name>.png`. It runs 4 Edge instances in parallel.

## Next steps
1. **Test the gallery pictures.** Run `python tools/web_snapshots.py --scratch <scratch>` and look at
   each picture in `docs/screenshots/web/controls/`. Fix:
   - samples whose popup does not open (`data-demo-open` handling in `gallery.js`)
   - crops that are wrong (texture or magenta at the edges)
   - animations that make the pictures differ between runs

   Compare with `docs/screenshots/controls/<same name>.png` (WPF).
2. **Web pages** in `docs/web/` (URL `/web/<name>.html`). Mirror the WPF pages: for each control a
   `##` heading, the picture `../screenshots/web/controls/<name>.png`, a tag helper example, the HTML
   it writes, and a table of attributes and events. Use `parent: Controls` +
   `grand_parent: JetPack for the web` for the controls pages.
   - `getting-started.md`: package, `_ViewImports`, layout (`asp-append-version`, script in `<head>`
     without defer), window frame (`body.jp-window`, title bar, status bar), Tailwind before/after,
     without Razor.
   - `theming.md`: `data-accent`, `ThemeManager.changeAccent` (name or #hex), `controlFontSize`,
     `showFocusVisuals`, `jp-accent-changed`, overriding `--jp-*` variables, texture/vignette.
   - `typography.md`: fonts, `jp-text-block` styles, h1–h6, icons (`jp-icon`, sprite, label).
   - `controls.md`, with children `window.md` (flyouts, dialogs, title bar modes, hamburger),
     `buttons.md`, `inputs.md`, `selection.md`, `lists.md`, `navigation.md`, `status.md`.
   - `reference.md`: all tag helpers with attributes (source: `src/JetPack.Web/TagHelpers/*.cs`),
     CSS classes, data attributes and `window.JetPack` API (header of `jetpack.js`), `jp-change`
     details per control.
   - `samples.md`: web demo, `/Demo/Gallery`, MVC sample.
   - Slim `docs/web.md` down to an overview with links (keep the URL; PACKAGE.md links to it).
3. **Tools:**
   - `tools/update_docs.py`: `check_doc_images` and `check_controls_documented` only glob `docs/*.md`;
     also include `docs/web/*.md` and allow `../screenshots/` paths. Warn about web gallery pictures
     no page shows.
   - `tools/generate_nav_headings.py`: globs `docs/*.md`; include `docs/web/*.md`.
   - Fix `docs/typography.md`'s link to `web.md` if needed.
4. **CLAUDE.md:** the web gallery (`/Demo/Gallery?shot=`), and that a new web control gets a gallery
   sample and a picture. Add the web calendar/datepicker pictures to the "changes daily" list.
5. **Finish:** run `python tools/update_docs.py --check` and `dotnet build JetPack.Wpf.sln`, then
   commit, push and open a PR. Check the three-level menu on the live site after the merge (no Jekyll
   locally).

## Known bug spotted on the way
`MenuItemTagHelper` and `HamburgerMenuTagHelper` write the icon href as
`/_content/JetPack.Web/img/icons.svg` without `Request.PathBase`, unlike `IconTagHelper`. Fix
separately.
