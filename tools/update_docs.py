"""Refreshes the generated parts of the documentation.

Usage (from the repository root):

    python tools/update_docs.py              build, render and copy everything
    python tools/update_docs.py --check      only the checks, no build or screenshots

Steps:
  1. Builds the demo and the sample apps into a scratch folder (never into bin/, which Visual Studio
     may have locked).
  2. Runs each app with --snapshot and checks that no picture is blank. RenderTargetBitmap returns
     transparent images while the screen is off or the session is locked.
  3. Copies the pictures to docs/screenshots, docs/screenshots/controls and docs/screenshots/samples.
  4. Regenerates docs/resource-keys.md and docs/_data/nav_headings.yml (the headings in the docs menu).
  5. Checks that every {StaticResource} in Styles/ is defined earlier in the same file, that every
     image the docs reference exists, and that every gallery picture is used by a docs page.

Prints a report and exits with 1 when a step or a check fails. Needs only the Python standard library
and the .NET SDK.
"""
import argparse
import filecmp
import glob
import os
import re
import shutil
import struct
import subprocess
import sys
import tempfile
import zlib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DOCS = os.path.join(ROOT, 'docs')
SHOTS = os.path.join(DOCS, 'screenshots')

# Project, executable name, and how its --snapshot output maps onto docs/screenshots.
APPS = [
    ('src/JetPack.Wpf.Demo', 'JetPack.Wpf.Demo'),
    ('samples/JetPack.Wpf.Samples.Admin', 'JetPack.Wpf.Samples.Admin'),
    ('samples/JetPack.Wpf.Samples.Portal', 'JetPack.Wpf.Samples.Portal'),
]

# Demo page pictures (numbered by the demo) and the names the docs use.
DEMO_PAGES = {
    '01-core.png': 'core.png',
    '02-sdk.png': 'sdk.png',
    '03-toolkit.png': 'toolkit.png',
    '04-extras.png': 'extras.png',
    '05-jetpackgrid.png': 'jetpackgrid.png',
    '06-window.png': 'window.png',
    '07-palette.png': 'palette.png',
    '20-core-orange.png': 'core-orange.png',
    '22-window-light.png': 'window-light.png',
    '23-navigation-bar.png': 'navigation-bar.png',
    '24-dialog.png': 'dialog.png',
    '25-flyout.png': 'flyout.png',
}


class Report:
    def __init__(self):
        self.errors = []
        self.warnings = []
        self.added = []
        self.changed = []

    def error(self, text):
        self.errors.append(text)
        print('  ERROR ' + text)

    def warn(self, text):
        self.warnings.append(text)
        print('  warning ' + text)


def step(text):
    print('\n== ' + text)


# ---------------------------------------------------------------- build and render

def build_and_render(scratch, report):
    folders = {}
    for project, exe in APPS:
        out = os.path.join(scratch, 'bin', exe)
        snap = os.path.join(scratch, 'snap', exe)
        step(f'Building {project}')
        result = subprocess.run(['dotnet', 'build', os.path.join(ROOT, project), '-c', 'Release', '-o', out, '-nologo', '-v', 'q'],
                                capture_output=True, text=True)
        if result.returncode != 0:
            report.error(f'build of {project} failed:\n{result.stdout[-3000:]}{result.stderr[-2000:]}')
            continue

        step(f'Rendering {exe} --snapshot')
        shutil.rmtree(snap, ignore_errors=True)
        try:
            subprocess.run([os.path.join(out, exe + '.exe'), '--snapshot', snap], timeout=600, check=False)
        except subprocess.TimeoutExpired:
            report.error(f'{exe} --snapshot did not finish within 10 minutes')
            continue
        error_file = os.path.join(snap, 'error.txt')
        if os.path.exists(error_file):
            report.error(f'{exe} --snapshot failed:\n' + open(error_file, encoding='utf-8', errors='replace').read())
            continue
        pictures = glob.glob(os.path.join(snap, '**', '*.png'), recursive=True)
        print(f'  {len(pictures)} pictures')
        blank = [p for p in pictures if is_blank(p)]
        if blank:
            report.error(f'{len(blank)} of {len(pictures)} pictures from {exe} are blank. The screen is probably off or '
                         'the session locked; turn the screen on (or connect with RDP) and run again.')
            continue
        folders[exe] = snap
    return folders


def is_blank(path):
    """True when every pixel of an RGBA PNG is fully transparent (filtered bytes of a blank image are all 0)."""
    with open(path, 'rb') as f:
        data = f.read()
    pos, idat, width, height = 8, b'', 0, 0
    while pos < len(data):
        length, kind = struct.unpack('>I4s', data[pos:pos + 8])
        chunk = data[pos + 8:pos + 8 + length]
        if kind == b'IHDR':
            width, height = struct.unpack('>II', chunk[:8])
        elif kind == b'IDAT':
            idat += chunk
        pos += 12 + length
    raw = zlib.decompress(idat)
    stride = len(raw) // height if height else 0
    return all(not any(raw[row * stride + 1:(row + 1) * stride]) for row in range(height))


def copy_pictures(folders, report):
    step('Copying pictures to docs/screenshots')
    plan = []
    demo = folders.get('JetPack.Wpf.Demo')
    if demo:
        for source, target in DEMO_PAGES.items():
            plan.append((os.path.join(demo, source), os.path.join(SHOTS, target)))
        for source in glob.glob(os.path.join(demo, 'controls', '*.png')):
            plan.append((source, os.path.join(SHOTS, 'controls', os.path.basename(source))))
    for exe in ('JetPack.Wpf.Samples.Admin', 'JetPack.Wpf.Samples.Portal'):
        if exe in folders:
            for source in glob.glob(os.path.join(folders[exe], '*.png')):
                plan.append((source, os.path.join(SHOTS, 'samples', os.path.basename(source))))

    for source, target in plan:
        if not os.path.exists(source):
            report.error(f'expected picture {os.path.basename(source)} was not rendered')
            continue
        os.makedirs(os.path.dirname(target), exist_ok=True)
        name = os.path.relpath(target, DOCS).replace('\\', '/')
        if not os.path.exists(target):
            report.added.append(name)
        elif not filecmp.cmp(source, target, shallow=False):
            report.changed.append(name)
        else:
            continue
        shutil.copyfile(source, target)
    print(f'  {len(report.added)} new, {len(report.changed)} changed, {len(plan) - len(report.added) - len(report.changed)} unchanged')


# ---------------------------------------------------------------- checks

def regenerate_resource_keys(report):
    step('Regenerating docs/resource-keys.md')
    result = subprocess.run([sys.executable, os.path.join(ROOT, 'tools', 'generate_resource_keys.py')], capture_output=True, text=True)
    if result.returncode != 0:
        report.error('generate_resource_keys.py failed:\n' + result.stdout + result.stderr)


def regenerate_nav_headings(report):
    step('Regenerating docs/_data/nav_headings.yml')
    result = subprocess.run([sys.executable, os.path.join(ROOT, 'tools', 'generate_nav_headings.py')], capture_output=True, text=True)
    if result.returncode != 0:
        report.error('generate_nav_headings.py failed:\n' + result.stdout + result.stderr)


def check_static_resources(report):
    """StaticResource and BasedOn across the Controls.*.xaml files fail at runtime (each is loaded on its own)."""
    step('Checking {StaticResource} order in Styles/')
    key_pattern = re.compile(r'x:Key="([^"{]+)"')
    use_pattern = re.compile(r'\{StaticResource\s+([^}\s]+)\}')
    count = 0
    for path in sorted(glob.glob(os.path.join(ROOT, 'src', 'JetPack.Wpf', 'Styles', '**', '*.xaml'), recursive=True)):
        text = open(path, encoding='utf-8').read()
        defined = {m.group(1): m.start() for m in key_pattern.finditer(text)}
        for m in use_pattern.finditer(text):
            key = m.group(1)
            if key.startswith('{') or key.startswith('x:'):
                continue
            count += 1
            line = text.count('\n', 0, m.start()) + 1
            where = f'{os.path.relpath(path, ROOT)}:{line}'
            if key not in defined:
                report.error(f'{where}: StaticResource {key} is not defined in this file (use DynamicResource)')
            elif defined[key] > m.start():
                report.error(f'{where}: StaticResource {key} is used before it is defined')
    print(f'  {count} references checked')


def check_doc_images(report):
    step('Checking images in docs/')
    referenced = set()
    for page in glob.glob(os.path.join(DOCS, '*.md')):
        text = open(page, encoding='utf-8').read()
        for m in re.finditer(r'!\[[^\]]*\]\((screenshots/[^)\s]+)\)', text):
            referenced.add(m.group(1))
            if not os.path.exists(os.path.join(DOCS, m.group(1))):
                report.error(f'{os.path.basename(page)} shows {m.group(1)}, which does not exist')
    for root_md in ('README.md', os.path.join('src', 'JetPack.Wpf', 'PACKAGE.md')):
        path = os.path.join(ROOT, root_md)
        if os.path.exists(path):
            referenced.update(m.group(1) for m in re.finditer(r'(screenshots/[\w/.-]+\.png)', open(path, encoding='utf-8').read()))

    for picture in sorted(glob.glob(os.path.join(SHOTS, 'controls', '*.png')) + glob.glob(os.path.join(SHOTS, 'samples', '*.png'))):
        name = os.path.relpath(picture, DOCS).replace('\\', '/')
        if name not in referenced:
            report.warn(f'{name} is not shown on any docs page')

    gallery = open(os.path.join(ROOT, 'src', 'JetPack.Wpf.Demo', 'Views', 'GalleryPage.xaml'), encoding='utf-8').read()
    tags = set(re.findall(r'Style="\{StaticResource Demo\.Sample\}" Tag="([^"]+)"', gallery))
    for picture in glob.glob(os.path.join(SHOTS, 'controls', '*.png')):
        tag = os.path.splitext(os.path.basename(picture))[0]
        if tag not in tags:
            report.warn(f'screenshots/controls/{tag}.png has no sample in GalleryPage.xaml any more')
    print(f'  {len(referenced)} references, {len(tags)} gallery samples')


def check_controls_documented(report):
    """Every public control class should be mentioned on a docs page."""
    step('Checking that every control is documented')
    pages = '\n'.join(open(p, encoding='utf-8').read() for p in glob.glob(os.path.join(DOCS, '*.md')))
    classes = []
    for path in glob.glob(os.path.join(ROOT, 'src', 'JetPack.Wpf', 'Controls', '*.cs')):
        classes += re.findall(r'^public (?:sealed |abstract )?class (\w+)\s*:\s*(?:\w+)', open(path, encoding='utf-8').read(), re.M)
    for name in sorted(set(classes)):
        if f'`{name}`' not in pages and f'jp:{name}' not in pages:
            report.warn(f'control {name} is not mentioned in docs/')
    print(f'  {len(set(classes))} controls')


# ---------------------------------------------------------------- main

def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--check', action='store_true', help='only run the checks; no build, no screenshots')
    parser.add_argument('--scratch', default=os.path.join(tempfile.gettempdir(), 'jetpack-docs'),
                        help='folder for builds and raw snapshots (default: %(default)s)')
    args = parser.parse_args()
    report = Report()

    if not args.check:
        folders = build_and_render(args.scratch, report)
        copy_pictures(folders, report)
    regenerate_resource_keys(report)
    regenerate_nav_headings(report)
    check_static_resources(report)
    check_doc_images(report)
    check_controls_documented(report)

    step('Summary')
    for title, items in (('New pictures', report.added), ('Changed pictures', report.changed)):
        if items:
            print(f'{title}:')
            for item in items:
                print('  docs/' + item)
    print(f'{len(report.errors)} error(s), {len(report.warnings)} warning(s)')
    if not args.check:
        print(f'Raw snapshots: {os.path.join(args.scratch, "snap")}')
    return 1 if report.errors else 0


if __name__ == '__main__':
    sys.exit(main())
