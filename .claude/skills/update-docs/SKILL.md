---
name: update-docs
description: Bring the JetPack documentation up to date with the code - screenshots of the demo, the control gallery and the sample apps, docs/resource-keys.md, and the docs pages for anything that changed. Use before a release (a v*.*.* tag) or after larger changes to controls, styles or the demo, or when the user asks to update the docs or the screenshots.
---

# Update the documentation

The mechanical part is `tools/update_docs.py`; your part is to look at the result and make the text
match the code. Work in the repository root.

## 1. Run the script

```bash
python tools/update_docs.py
```

It builds the demo and both sample apps into a scratch folder (never into `bin/`, which Visual Studio
may have locked), runs them with `--snapshot`, copies the pictures into `docs/screenshots/`,
regenerates `docs/resource-keys.md` and runs the checks. It takes about a minute.

- **"pictures ... are blank"**: the screen is off or the session is locked, and WPF then renders
  nothing. Ask the user to turn the screen on or connect with RDP, then run it again. Do not commit
  anything from a blank run.
- **Build errors**: fix them first; the docs must describe code that builds.
- **StaticResource errors**: a `{StaticResource}` in `Styles/` that is not defined earlier in the same
  file fails at runtime. Change it to `DynamicResource` (or move the resource) as CLAUDE.md describes.
- `python tools/update_docs.py --check` runs only the checks (no build, no pictures), which is enough
  after text-only edits.

## 2. Look at what changed

The script lists new and changed pictures. Endless animations are frozen for the snapshots, so a
picture only changes when the look changed. Read every new or changed PNG and check it against the
change that caused it: no clipped text, no blank areas, open drop-downs where the gallery marks them
(`d:Demo.Open`), and nothing showing the user's desktop. If a picture is wrong, fix the cause (the
style, the gallery sample in `src/JetPack.Wpf.Demo/Views/GalleryPage.xaml`, or a sample app page)
and run the script again.

## 3. Check the text against the code

Find what changed since the last release:

```bash
git describe --tags --abbrev=0
git diff --stat <tag>..HEAD -- src/JetPack.Wpf samples
git diff <tag>..HEAD -- docs/resource-keys.md
```

Then, for each change:

- **New or renamed control, property or style key**: the matching page in `docs/` (see the table in
  `docs/README.md`) describes it, with a XAML example and, for properties, the table row. Give a new
  control a sample in `GalleryPage.xaml` (`Style="{StaticResource Demo.Sample}" Tag="<file name>"`)
  and show `screenshots/controls/<file name>.png` under its heading. The script warns about controls
  that no page mentions and gallery pictures that no page shows.
- **Changed look or behaviour**: the description under the control's heading still matches the new
  picture (colours, sizes, what happens on hover or click).
- **Removed things**: no page mentions them any more.
- `README.md` and `src/JetPack.Wpf/PACKAGE.md` (the nuget.org page; absolute image URLs only) still
  match, including the list of own controls.
- `CLAUDE.md` gets any new pitfall that was found along the way.

Everything in the repository is English, also when the user writes in Danish.

## 4. Finish

Run `python tools/update_docs.py --check` once more; it must end with 0 errors. Tell the user what
changed (pictures and pages), what you fixed, and anything you could not verify. Commit or push only
when the user asks; the docs changes belong in the same commit as the code they describe when that
code is not committed yet.
