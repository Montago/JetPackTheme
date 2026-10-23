"""Generates the window ground image: Assets/Texture.png.

It is drawn from scratch with the numbers below, so it is covered by the MIT License like the rest
of the repository. The vignette over it is drawn in code (src/JetPack.Wpf/Controls/Vignette.cs).

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



def texture():
    rng = np.random.default_rng(20100415)
    noise = rng.normal(0, TEXTURE_SPREAD, (TEXTURE_SIZE, TEXTURE_SIZE))
    noise = np.clip(np.rint(noise), -TEXTURE_RANGE, TEXTURE_RANGE)
    rgb = np.stack([noise + c for c in TEXTURE_BASE], axis=-1)
    return Image.fromarray(rgb.astype(np.uint8), 'RGB')



if __name__ == '__main__':
    os.makedirs(out, exist_ok=True)
    # Only a handful of distinct colours, so a palette PNG is lossless and much smaller
    texture().quantize(colors=16, method=Image.Quantize.MAXCOVERAGE).save(
        os.path.join(out, 'Texture.png'), optimize=True)
    print('Wrote Texture.png to', out)
