"""Generates the window ground images: Assets/Texture.png and Assets/Vignette.png.

Both are drawn from scratch with the numbers below, so they are covered by the MIT License
like the rest of the repository.

Usage (from the repository root): python tools/generate_backgrounds.py [output folder]
Requires numpy and Pillow.
"""
import os
import sys

import numpy as np
from PIL import Image

root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(root, 'src', 'JetPack.Wpf', 'Assets')

# Texture: grey with fine noise, tiled every 200 px (Brushes.xaml uses Viewport 0,0,200,200).
# Independent per-pixel noise has no seams, so any square tiles cleanly.
TEXTURE_SIZE = 200
TEXTURE_BASE = (133, 135, 140)      # mean colour, #85878C (blue-grey, close to Window.Ground)
TEXTURE_SPREAD = 2.2                # standard deviation of the noise, in 0-255 steps
TEXTURE_RANGE = 4                   # clip the noise to +/- this many steps

# Vignette: black, darkening every edge; stretched over the whole window (Stretch=Fill).
VIGNETTE_SIZE = 1000
VIGNETTE_EDGE = 0.30                # opacity at the middle of an edge
VIGNETTE_REACH = 0.17               # how far in (fraction of the size) the shade reaches
VIGNETTE_CURVE = 1.9                # falloff exponent; higher fades faster near the edge
VIGNETTE_CORNER = 1.6               # p-norm used to join two edges; lower darkens corners more


def texture():
    rng = np.random.default_rng(20100415)
    noise = rng.normal(0, TEXTURE_SPREAD, (TEXTURE_SIZE, TEXTURE_SIZE))
    noise = np.clip(np.rint(noise), -TEXTURE_RANGE, TEXTURE_RANGE)
    rgb = np.stack([noise + c for c in TEXTURE_BASE], axis=-1)
    return Image.fromarray(rgb.astype(np.uint8), 'RGB')


def vignette():
    t = (np.arange(VIGNETTE_SIZE) + 0.5) / VIGNETTE_SIZE
    edge = np.minimum(t, 1 - t)                                 # distance to the nearest edge
    shade = VIGNETTE_EDGE * np.clip(1 - edge / VIGNETTE_REACH, 0, 1) ** VIGNETTE_CURVE
    sx, sy = np.meshgrid(shade, shade)
    p = VIGNETTE_CORNER
    alpha = (sx ** p + sy ** p) ** (1 / p)
    rgba = np.zeros((VIGNETTE_SIZE, VIGNETTE_SIZE, 4), np.uint8)
    rgba[..., 3] = np.rint(np.clip(alpha, 0, 1) * 255).astype(np.uint8)
    return Image.fromarray(rgba, 'RGBA')


if __name__ == '__main__':
    os.makedirs(out, exist_ok=True)
    # Only a handful of distinct colours, so a palette PNG is lossless and much smaller
    texture().quantize(colors=16, method=Image.Quantize.MAXCOVERAGE).save(
        os.path.join(out, 'Texture.png'), optimize=True)
    vignette().save(os.path.join(out, 'Vignette.png'), optimize=True)
    print('Wrote Texture.png and Vignette.png to', out)
