"""Regenerates docs/resource-keys.md from the theme dictionaries.

Usage (from the repository root): python tools/generate_resource_keys.py
"""
import glob
import re
import sys
import xml.etree.ElementTree as ET

import os
root = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
styles = root + '/src/JetPack.Wpf/Styles/'
X = '{http://schemas.microsoft.com/winfx/2006/xaml}'


def local(tag):
    return tag.split('}')[-1]


def describe(el):
    t = local(el.tag)
    if t == 'SolidColorBrush':
        return 'SolidColorBrush', f"`{el.get('Color')}`"
    if t in ('LinearGradientBrush', 'RadialGradientBrush'):
        stops = []
        for s in el.iter():
            if local(s.tag) == 'GradientStop':
                off = s.get('Offset') or '0'
                stops.append(f"`{s.get('Color')}` {off}")
        return t, ' → '.join(stops)
    if t == 'ImageBrush':
        return 'ImageBrush', f"`{el.get('ImageSource').split('/')[-1]}`"
    if t == 'Color':
        return 'Color', f"`{(el.text or '').strip()}`"
    if t == 'DropShadowEffect':
        parts = [f"{k} {el.get(k)}" for k in ('Color', 'BlurRadius', 'ShadowDepth', 'Direction', 'Opacity') if el.get(k)]
        return 'DropShadowEffect', ', '.join(parts)
    if t == 'FontFamily':
        return 'FontFamily', f"`{(el.text or '').strip()}`"
    if t == 'Double':
        return 'Double', f"`{(el.text or '').strip()}`"
    if t == 'String':
        return 'String', f"`{(el.text or '').strip()}`"
    return t, ''


def table(path, title, intro):
    tree = ET.parse(path)
    rows = []
    for el in tree.getroot():
        key = el.get(X + 'Key')
        if not key:
            continue
        kind, value = describe(el)
        rows.append(f"| `{key}` | {kind} | {value} |")
    return f"## {title}\n\n{intro}\n\n| Key | Type | Value |\n| --- | --- | --- |\n" + '\n'.join(rows) + '\n'


def style_keys():
    out = []
    for f in sorted(glob.glob(styles + 'Controls.*.xaml')):
        s = open(f, encoding='utf-8').read()
        keys = re.findall(r'x:Key="(JetPack\.Styles\.[^"]+)"[^>]*TargetType="([^"]+)"', s)
        keys += [(k, t) for t, k in re.findall(r'TargetType="([^"]+)"[^>]*x:Key="(JetPack\.Styles\.[^"]+)"', s)]
        for k, t in sorted(set(keys)):
            out.append(f"| `{k}` | `{t}` | {os.path.basename(f)} |")
    return out


doc = ["# Resource keys",
       "",
       "Every brush, colour, font size and effect the theme defines, generated from the XAML in",
       "`src/JetPack.Wpf/Styles`. Reference them with `{DynamicResource ...}` so they follow accent changes.",
       "",
       "To change a value app-wide, redefine the key in `App.xaml` after merging `JetPack.xaml`, for example",
       "`<SolidColorBrush x:Key=\"JetPack.Brushes.Error\" Color=\"#FFC00000\" />`.",
       ""]
doc.append(table(styles + 'Accents/Green.xaml', 'Accent (changes with ThemeManager.ChangeAccent)',
                 'Values shown are the default green accent. `Accents/Orange.xaml` and `Accents/Blue.xaml` define the same keys.'))
doc.append(table(styles + 'Brushes.xaml', 'Neutral brushes', 'Shared by every accent.'))
doc.append(table(styles + 'Fonts.xaml', 'Fonts', '`JetPack.Font.Size.Control` is what `ThemeManager.ControlFontSize` overrides.'))
doc.append(table(styles + 'Effects.xaml', 'Effects', 'Drop shadows and text effects used by the templates.'))
doc.append("## Named styles\n\nEvery standard control also gets an implicit style (no key needed). These keyed styles are the\n"
           "variants and building blocks; use them with `Style=\"{StaticResource ...}\"` or as `BasedOn`.\n\n"
           "| Key | Target type | File |\n| --- | --- | --- |\n" + '\n'.join(style_keys()) + '\n')
open(root + '/docs/resource-keys.md', 'w', encoding='utf-8').write('\n'.join(doc))
print('ok')
