"""Pictures of the web theme for the docs: every tab of src/JetPack.Web.Demo and every page of
samples/JetPack.Web.Samples.Mvc, taken with headless Microsoft Edge at 1280 × 900.

    docs/screenshots/web/<tab>.png              core, sdk, toolkit, extras, jetpackgrid, window, palette, icons
    docs/screenshots/web/controls/<name>.png    one per sample of the demo's /Demo/Gallery page
    docs/screenshots/samples/web-<page>.png     dashboard, employees, timesheets, projects

A gallery picture is /Demo/Gallery?shot=<name>: the page shows that sample on magenta, opens its
drop-down, menu or tool tip, and puts the texture behind the sample and its popups; the picture is
cut to the pixels that are not magenta.

Both sites are built to a scratch folder (so a running Visual Studio does not lock them), started on
their own ports, and stopped again. A picture is copied only when its pixels changed (PNGs differ in
their bytes on every run). Edge gets a fresh profile, so stored accents or settings do not leak in.

Usage (from the repository root): python tools/web_snapshots.py [--scratch <folder>]
Needs Pillow and Microsoft Edge.
"""
import argparse
import concurrent.futures
import io
import os
import re
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
     [(f'?snapshot#{tab}', os.path.join('web', tab + '.png')) for tab in ('core', 'sdk', 'toolkit', 'extras', 'jetpackgrid', 'window', 'palette', 'icons')]),
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


def edge_shot(browser, scratch, url, raw, size='1280,900'):
    # A fresh profile for every picture: an Edge that is still shutting down keeps its profile, and a
    # new Edge on the same profile hands the URL to it and quits without taking a picture. The old
    # picture is deleted first, and the new one waited for, so a stale or half-written file is never used.
    if os.path.exists(raw):
        os.remove(raw)
    profile = tempfile.mkdtemp(prefix='edge-profile-', dir=scratch)
    subprocess.run([browser, '--headless=new', '--disable-gpu', '--force-device-scale-factor=1', '--user-data-dir=' + profile,
                    '--window-size=' + size, '--virtual-time-budget=5000', '--screenshot=' + raw, url],
                   capture_output=True, timeout=120)
    wait_for_file(raw)
    shutil.rmtree(profile, ignore_errors=True)


def wait_for_file(path, seconds=60):
    """Until the picture exists and its size stands still (Edge may write it after its process exits)."""
    end, last = time.time() + seconds, -1
    while time.time() < end:
        size = os.path.getsize(path) if os.path.exists(path) else -1
        if size > 0 and size == last:
            return
        last = size
        time.sleep(0.5)
    raise RuntimeError(f'Edge did not write {path}')


def cut_out(raw):
    """The part of a gallery picture that is not magenta (#FF00FF)."""
    from PIL import Image, ImageChops
    image = Image.open(raw).convert('RGB')
    magenta = Image.new('RGB', image.size, (255, 0, 255))
    box = ImageChops.difference(image, magenta).getbbox()
    if box is None:
        raise RuntimeError(f'{raw} is all magenta')
    image.crop(box).save(raw)


def same_pixels(a, b):
    from PIL import Image, ImageChops
    x, y = Image.open(a).convert('RGBA'), Image.open(b).convert('RGBA')
    if x.size != y.size:
        return False
    # Edge's rasteriser differs between runs by one level, or in a few anti-aliased corner pixels
    # when four instances run at once; neither is a change of the look
    over = [c.point(lambda v: 255 if v > 1 else 0) for c in ImageChops.difference(x, y).split()]
    mask = over[0]
    for c in over[1:]:
        mask = ImageChops.lighter(mask, c)
    return mask.histogram()[255] <= 8


def copy_if_changed(raw, target, added, changed):
    dest = os.path.join(SHOTS, target)
    os.makedirs(os.path.dirname(dest), exist_ok=True)
    rel = os.path.relpath(dest, ROOT).replace('\\', '/')
    if not os.path.exists(dest):
        added.append(rel)
    elif same_pixels(raw, dest):
        return
    else:
        changed.append(rel)
    shutil.copyfile(raw, dest)


def gallery(browser, base, scratch, added, changed):
    with urllib.request.urlopen(base + 'Demo/Gallery', timeout=30) as r:
        names = re.findall(r'class="gallery-sample" data-shot="([^"]+)"', r.read().decode('utf-8'))
    print(f'   {len(names)} gallery samples')

    def one(name):
        raw = os.path.join(scratch, 'shots', 'web', 'controls', name + '.png')
        os.makedirs(os.path.dirname(raw), exist_ok=True)
        edge_shot(browser, scratch, f'{base}Demo/Gallery?shot={name}', raw)
        cut_out(raw)
        return name, raw

    # four Edge instances at a time
    with concurrent.futures.ThreadPoolExecutor(4) as pool:
        for name, raw in sorted(pool.map(one, names)):
            copy_if_changed(raw, os.path.join('web', 'controls', name + '.png'), added, changed)


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
                edge_shot(browser, args.scratch, base + path, raw)
                copy_if_changed(raw, target, added, changed)
            if name == 'JetPack.Web.Demo':
                gallery(browser, base, args.scratch, added, changed)
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
