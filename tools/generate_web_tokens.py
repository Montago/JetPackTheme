"""Writes the CSS custom properties of the web theme from the WPF dictionaries, so both share one
set of values: Brushes.xaml, Fonts.xaml, Effects.xaml and Accents/*.xaml become
src/JetPack.Web/wwwroot/css/jetpack.tokens.css, and Assets/Texture.png is copied next to it.

Key names: the "JetPack." prefix becomes "--jp-", the type part ("Brushes", "Colors", "Fonts",
"Font", "Effects") is dropped or shortened, and CamelCase becomes kebab-case:
    JetPack.Brushes.Control.Background.MouseOver -> --jp-control-background-mouse-over
    JetPack.Colors.Accent.TextShadow             -> --jp-color-accent-text-shadow
    JetPack.Font.Size.Control                    -> --jp-font-size-control
    JetPack.Effects.ControlShadow                -> --jp-effect-control-shadow  (a box-/text-shadow value)

Usage (from the repository root):
    python tools/generate_web_tokens.py           write the files
    python tools/generate_web_tokens.py --check   exit with 1 when the files are out of date
"""
import math
import os
import re
import shutil
import sys
import xml.etree.ElementTree as ET

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
STYLES = os.path.join(ROOT, 'src', 'JetPack.Wpf', 'Styles')
TEXTURE = os.path.join(ROOT, 'src', 'JetPack.Wpf', 'Assets', 'Texture.png')
OUT_DIR = os.path.join(ROOT, 'src', 'JetPack.Web', 'wwwroot')
OUT_CSS = os.path.join(OUT_DIR, 'css', 'jetpack.tokens.css')
OUT_TEXTURE = os.path.join(OUT_DIR, 'img', 'texture.png')
X = '{http://schemas.microsoft.com/winfx/2006/xaml}'

# Green is the default accent, as in ThemeManager
ACCENTS = ['Green', 'Orange', 'Blue']


def local(tag):
    return tag.split('}')[-1]


def kebab(part):
    return re.sub(r'(?<=[a-z0-9])(?=[A-Z])', '-', part).lower()


def css_name(key):
    parts = key.split('.')[1:]
    kind = parts[0]
    rest = parts[1:]
    prefix = {'Brushes': [], 'Colors': ['color'], 'Fonts': ['font'], 'Font': ['font'], 'Effects': ['effect']}.get(kind, [kind.lower()])
    return '--jp-' + '-'.join(prefix + [kebab(p) for p in rest])


def color(value):
    """#AARRGGBB or #RRGGBB -> #RRGGBB or #RRGGBBAA."""
    v = value.strip().lstrip('#')
    if len(v) == 8:
        a, rgb = v[:2], v[2:]
        return '#' + (rgb if a.upper() == 'FF' else rgb + a).upper()
    return '#' + v.upper()


def rgba(value, opacity):
    v = value.strip().lstrip('#')
    if len(v) == 8:
        v = v[2:]
    r, g, b = (int(v[i:i + 2], 16) for i in (0, 2, 4))
    return f'rgb({r} {g} {b} / {opacity:g})'


def point(text, default):
    if not text:
        return default
    x, y = (float(n) for n in text.split(','))
    return x, y


def stops(el):
    out = []
    for s in el.iter():
        if local(s.tag) == 'GradientStop':
            offset = float(s.get('Offset') or 0)
            out.append(f"{color(s.get('Color'))} {offset * 100:g}%")
    return ', '.join(out)


def linear(el):
    (x0, y0), (x1, y1) = point(el.get('StartPoint'), (0, 0)), point(el.get('EndPoint'), (1, 1))
    dx, dy = x1 - x0, y1 - y0
    # WPF measures in the element's bounding box, as CSS "to <corner>" does for diagonals
    if dx == 0:
        direction = '' if dy > 0 else 'to top, '
    elif dy == 0:
        direction = 'to right, ' if dx > 0 else 'to left, '
    elif abs(dx) == abs(dy):
        direction = f"to {'bottom' if dy > 0 else 'top'} {'right' if dx > 0 else 'left'}, "
    else:
        direction = f'{math.degrees(math.atan2(dx, -dy)):.1f}deg, '
    return f'linear-gradient({direction}{stops(el)})'


def radial(el):
    cx, cy = point(el.get('Center'), (0.5, 0.5))
    rx, ry = float(el.get('RadiusX') or 0.5), float(el.get('RadiusY') or 0.5)
    # GradientOrigin has no CSS counterpart; the centre is close enough for these brushes
    return f'radial-gradient({rx * 100:g}% {ry * 100:g}% at {cx * 100:g}% {cy * 100:g}%, {stops(el)})'


def shadow(el):
    depth = float(el.get('ShadowDepth') or 5)
    direction = math.radians(float(el.get('Direction') or 315))
    blur = float(el.get('BlurRadius') or 5)
    x, y = (round(n, 2) + 0 for n in (depth * math.cos(direction), -depth * math.sin(direction)))
    tint = rgba(el.get('Color') or '#FF000000', float(el.get('Opacity') or 1))
    # CSS blur is about half of WPF's BlurRadius
    return f'{px(x)} {px(y)} {px(blur / 2)} {tint}'


def px(n):
    return '0' if n == 0 else f'{n:g}px'


def value(el):
    t = local(el.tag)
    text = (el.text or '').strip()
    if t == 'SolidColorBrush':
        return color(el.get('Color'))
    if t == 'Color':
        return color(text)
    if t == 'LinearGradientBrush':
        return linear(el)
    if t == 'RadialGradientBrush':
        return radial(el)
    if t == 'ImageBrush':
        return 'url("../img/texture.png")'
    if t == 'DropShadowEffect':
        return shadow(el)
    if t == 'FontFamily':
        generic = 'monospace' if el.get(X + 'Key').endswith('Mono') else 'sans-serif'
        return ', '.join(f'"{f.strip()}"' if ' ' in f.strip() else f.strip() for f in text.split(',')) + ', ' + generic
    if t == 'Double':
        return f'{float(text):g}px'
    return None


def block(path, selector):
    lines = []
    for el in ET.parse(path).getroot():
        key = el.get(X + 'Key')
        css = value(el) if key else None
        if css is not None:
            lines.append(f'  {css_name(key)}: {css};')
    return f'{selector} {{\n' + '\n'.join(lines) + '\n}\n'


def render():
    parts = [
        '/* Generated by tools/generate_web_tokens.py from src/JetPack.Wpf/Styles; do not edit. */\n',
        '/* Neutral values (Brushes.xaml, Fonts.xaml, Effects.xaml) */\n',
        block(os.path.join(STYLES, 'Brushes.xaml'), ':root'),
        block(os.path.join(STYLES, 'Fonts.xaml'), ':root'),
        block(os.path.join(STYLES, 'Effects.xaml'), ':root'),
        '/* Accents: set data-accent on <html> (or any element) to switch; green is the default */\n',
    ]
    for i, name in enumerate(ACCENTS):
        selector = f'[data-accent="{name.lower()}"]'
        if i == 0:
            selector = ':root, ' + selector
        parts.append(block(os.path.join(STYLES, 'Accents', name + '.xaml'), selector))
    return '\n'.join(parts)


def main():
    css = render()
    if '--check' in sys.argv:
        current = open(OUT_CSS, encoding='utf-8').read() if os.path.exists(OUT_CSS) else ''
        same_texture = os.path.exists(OUT_TEXTURE) and open(TEXTURE, 'rb').read() == open(OUT_TEXTURE, 'rb').read()
        if current != css or not same_texture:
            print('jetpack.tokens.css or texture.png is out of date; run python tools/generate_web_tokens.py')
            sys.exit(1)
        return
    os.makedirs(os.path.dirname(OUT_CSS), exist_ok=True)
    os.makedirs(os.path.dirname(OUT_TEXTURE), exist_ok=True)
    with open(OUT_CSS, 'w', encoding='utf-8', newline='\n') as f:
        f.write(css)
    shutil.copyfile(TEXTURE, OUT_TEXTURE)
    print(f'wrote {os.path.relpath(OUT_CSS, ROOT)} and {os.path.relpath(OUT_TEXTURE, ROOT)}')


if __name__ == '__main__':
    main()
