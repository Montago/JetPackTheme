"""Pictures of the web theme for the docs: every tab of src/JetPack.Web.Demo and every page of
samples/JetPack.Web.Samples.Mvc, taken with headless Microsoft Edge at 1280 × 900.

    docs/screenshots/web/<tab>.png              core, sdk, toolkit, extras, window, palette, icons
    docs/screenshots/samples/web-<page>.png     dashboard, employees, timesheets, projects

Both sites are built to a scratch folder (so a running Visual Studio does not lock them), started on
their own ports, and stopped again. A picture is copied only when its pixels changed (PNGs differ in
their bytes on every run). Edge gets a fresh profile, so stored accents or settings do not leak in.

Usage (from the repository root): python tools/web_snapshots.py [--scratch <folder>]
Needs Pillow and Microsoft Edge.
"""
import argparse
import io
import os
import shutil
import subprocess
import sys
import tempfile
import time
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SHOTS = os.path.join(ROOT, 'docs', 'screenshots')
EDGE = [r'C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe', r'C:\Program Files\Microsoft\Edge\Application\msedge.exe']

SITES = [
    ('src/JetPack.Web.Demo', 'JetPack.Web.Demo', 5291,
     [(f'#{tab}', os.path.join('web', tab + '.png')) for tab in ('core', 'sdk', 'toolkit', 'extras', 'window', 'palette', 'icons')]),
    ('samples/JetPack.Web.Samples.Mvc', 'JetPack.Web.Samples.Mvc', 5292,
     [(path, os.path.join('samples', 'web-' + name + '.png')) for name, path in
      (('dashboard', ''), ('employees', 'Employees'), ('timesheets', 'Timesheets'), ('projects', 'Projects'))]),
]


def edge():
    for path in EDGE:
        if os.path.exists(path):
            return path
    sys.exit('Microsoft Edge was not found')


def wait_for(url, seconds=90):
    end = time.time() + seconds
    while time.time() < end:
        try:
            with urllib.request.urlopen(url, timeout=5) as r:
                if r.status == 200:
                    return
        except OSError:
            time.sleep(1)
    raise TimeoutError(url)


def same_pixels(a, b):
    from PIL import Image, ImageChops
    x, y = Image.open(a).convert('RGBA'), Image.open(b).convert('RGBA')
    if x.size != y.size:
        return False
    d = ImageChops.difference(x, y)
    # getbbox() of an RGBA image only looks at alpha, so check the colours as well
    return d.convert('RGB').getbbox() is None and d.getchannel('A').getbbox() is None


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--scratch', default=os.path.join(tempfile.gettempdir(), 'jetpack-web-shots'))
    args = parser.parse_args()
    browser = edge()
    changed, added = [], []
    for project, name, port, pages in SITES:
        out = os.path.join(args.scratch, name)
        print(f'== {name}')
        build = subprocess.run(['dotnet', 'build', os.path.join(ROOT, project), '-c', 'Release', '-o', out, '-nologo', '-v', 'q'],
                               capture_output=True, text=True)
        if build.returncode != 0:
            sys.exit(build.stdout + build.stderr)
        base = f'http://localhost:{port}/'
        env = dict(os.environ, ASPNETCORE_ENVIRONMENT='Development')
        server = subprocess.Popen(['dotnet', os.path.join(out, name + '.dll'), '--urls', base],
                                  cwd=os.path.join(ROOT, project), env=env, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        try:
            wait_for(base)
            for path, target in pages:
                raw = os.path.join(args.scratch, 'shots', target)
                os.makedirs(os.path.dirname(raw), exist_ok=True)
                profile = os.path.join(args.scratch, 'edge-profile')
                shutil.rmtree(profile, ignore_errors=True)
                subprocess.run([browser, '--headless=new', '--disable-gpu', '--force-device-scale-factor=1', '--user-data-dir=' + profile,
                                '--window-size=1280,900', '--virtual-time-budget=5000', '--screenshot=' + raw, base + path],
                               capture_output=True, timeout=120)
                dest = os.path.join(SHOTS, target)
                os.makedirs(os.path.dirname(dest), exist_ok=True)
                rel = os.path.relpath(dest, ROOT).replace('\\', '/')
                if not os.path.exists(dest):
                    added.append(rel)
                elif same_pixels(raw, dest):
                    continue
                else:
                    changed.append(rel)
                shutil.copyfile(raw, dest)
        finally:
            server.terminate()
            server.wait(timeout=30)
    for title, items in (('New pictures', added), ('Changed pictures', changed)):
        if items:
            print(title + ':')
            for item in items:
                print('  ' + item)
    print(f'{len(added)} new, {len(changed)} changed')


if __name__ == '__main__':
    main()
