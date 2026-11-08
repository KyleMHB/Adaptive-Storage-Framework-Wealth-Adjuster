"""Render About/Preview.png from Adaptive Storage Framework's Workshop preview (artwork/asf-preview-source.png).

Removes the source's "1.5" corner badge with nearby starfield and adds a band naming this patch.
Uses RimWordFont from the Everything Has Quality checkout (not redistributed).

    python scripts/render-preview.py [--font PATH]
"""
import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--font', type=Path, default=ROOT.parent / 'EverythingHasQualityReforked' / 'artifacts' / 'steam-page-mockup' / 'RimWordFont.ttf')
args = parser.parse_args()

NAVY, CREAM, GOLD = (18, 34, 47), (244, 238, 222), (236, 200, 105)

src = Image.open(ROOT / 'artwork' / 'asf-preview-source.png').convert('RGB')
img = src.resize((640, 360), Image.LANCZOS)
sx, sy = 640 / src.width, 360 / src.height

# Corner badge: triangle (0,0)-(123,0)-(0,118) in source pixels. Cover it with starfield from just below it.
tri = [(0, 0), (round(130 * sx), 0), (0, round(125 * sy))]
mask = Image.new('L', img.size, 0)
ImageDraw.Draw(mask).polygon(tri, fill=255)
mask = mask.filter(ImageFilter.GaussianBlur(2))
patch_box = (round(30 * sx), round(130 * sy), round(30 * sx) + round(140 * sx), round(130 * sy) + round(135 * sy))
patch = img.crop(patch_box)
fill = Image.new('RGB', img.size)
fill.paste(patch, (0, 0))
img = Image.composite(fill, img, mask)


def font(size):
    return ImageFont.truetype(str(args.font), size)


def label(text):
    # RimWordFont draws capital O as an emblem; its lowercase o is the plain letter.
    return text.upper().replace('O', 'o')


img = img.convert('RGBA')
TOP, BOTTOM = (0, 0, 640, 64), (0, 300, 640, 360)
shade = Image.new('RGBA', img.size, (0, 0, 0, 0))
d = ImageDraw.Draw(shade)
d.rectangle(TOP, fill=NAVY + (235,))
d.rectangle((0, TOP[3] - 4, 640, TOP[3]), fill=GOLD + (255,))
d.rectangle(BOTTOM, fill=NAVY + (235,))
d.rectangle((0, BOTTOM[1], 640, BOTTOM[1] + 4), fill=GOLD + (255,))
img.alpha_composite(shade)
d = ImageDraw.Draw(img)

# Top: mod name with a PATCH tag.
title, tag = label('Wealth Adjuster'), label('patch')
size = 44
while True:
    tf, gf = font(size), font(round(size * .55))
    gap, pad_x = round(size * .35), round(size * .25)
    tw, gw = d.textlength(title, font=tf), d.textlength(tag, font=gf)
    if tw + gap + gw + pad_x * 2 <= 590:
        break
    size -= 1
left = (640 - (tw + gap + gw + pad_x * 2)) / 2
cy = (TOP[1] + TOP[3] - 4) // 2 + 1
d.text((left, cy), title, font=tf, fill=GOLD, anchor='lm')
px = left + tw + gap
h = round(size * .4)
d.rounded_rectangle((px, cy - h, px + gw + pad_x * 2, cy + h), radius=8, outline=CREAM, width=2)
d.text((px + pad_x, cy + 1), tag, font=gf, fill=CREAM, anchor='lm')

# Bottom: what it does.
line = label('Items in ASF storage count for less wealth')
size = 30
while d.textlength(line, font=font(size)) > 600:
    size -= 1
d.text((320, (BOTTOM[1] + 4 + BOTTOM[3]) // 2 + 1), line, font=font(size), fill=CREAM, anchor='mm')
img.convert('RGB').save(ROOT / 'About' / 'Preview.png', optimize=True)
print('Rendered About/Preview.png from the ASF preview')
