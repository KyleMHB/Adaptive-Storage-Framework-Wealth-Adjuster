"""Render the Workshop gallery image from RimWordFont and original RimWorld sprites.

Sprites are read from the local game's Assets.zip (1.7 stores them as zstd-compressed `.dds.split` entries).
The font and extracted sprites are local inputs and are not redistributed.

    python scripts/render-workshop-art.py [--archive PATH] [--font PATH] [--zstd-path DIR]
"""
import argparse
import io
import os
import shutil
import subprocess
import tempfile
import struct
import sys
import zipfile
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
EHQ = ROOT.parent / 'EverythingHasQualityReforked' / 'artifacts' / 'steam-page-mockup'
parser = argparse.ArgumentParser()
parser.add_argument('--archive', type=Path, default=Path(r'E:\Steam\steamapps\common\RimWorld\Assets.zip'))
parser.add_argument('--font', type=Path, default=EHQ / 'RimWordFont.ttf')
parser.add_argument('--zstd-path', type=Path, default=EHQ / 'tools')
args = parser.parse_args()
sys.path.insert(0, str(args.zstd_path))
import zstandard  # noqa: E402

NAVY, CREAM, GOLD, GREEN, GREY = (18, 34, 47), (244, 238, 222), (236, 200, 105), (108, 170, 120), (92, 104, 112)
STRIPE = [(205, 125, 120), (190, 165, 135), (238, 230, 205), (128, 182, 132), (120, 190, 210), (190, 160, 215), (236, 200, 105)]
STEEL_VALUE, STACK = 1.9, 75  # Core ThingDef Steel BaseMarketValue; stack used in the mod's tests.


GAME = args.archive.parent
STAGE = ROOT / 'work' / 'ddsrt'
JOIN_TOOL = ROOT / 'work' / 'ddsjoin' / 'RimWorld.dll'


def stage_runtime():
    """Hardlink the game's self-contained .NET 10 runtime next to scripts/ddsjoin, which calls Waylith's own
    DdsFile.TryJoin. 1.7 stores BC textures as .dds.split (endpoint and index bytes apart); Pillow needs plain DDS."""
    if not JOIN_TOOL.is_file():
        raise SystemExit('build scripts/ddsjoin first: dotnet build scripts/ddsjoin -c Release')
    STAGE.mkdir(parents=True, exist_ok=True)
    for f in list(GAME.glob('*.dll')) + [GAME / 'RimWorld.exe', GAME / 'RimWorld.runtimeconfig.json']:
        if f.name in ('RimWorld.dll', 'RimWorld.deps.json'):
            continue
        target = STAGE / f.name
        if not target.exists():
            try:
                os.link(f, target)
            except OSError:
                shutil.copy2(f, target)
    shutil.copy2(JOIN_TOOL, STAGE / 'RimWorld.dll')


def join_split(raw):
    with tempfile.TemporaryDirectory() as tmp:
        src, dst = Path(tmp) / 'in.split', Path(tmp) / 'out.dds'
        src.write_bytes(raw)
        subprocess.run([str(STAGE / 'RimWorld.exe'), str(GAME / 'Waylith.dll'), str(src), str(dst)], check=True)
        return dst.read_bytes()


def extract(archive, stream, path):
    for name in ('Resources/Textures/' + path + '.split', 'Resources/Textures/' + path):
        try:
            info = archive.getinfo(name)
        except KeyError:
            continue
        stream.seek(info.header_offset)
        name_size, extra_size = struct.unpack_from('<HH', stream.read(30), 26)
        stream.seek(name_size + extra_size, 1)
        data = stream.read(info.compress_size)
        raw = zstandard.ZstdDecompressor().decompress(data, max_output_size=info.file_size) if info.compress_type == 93 else archive.read(info)
        if name.endswith('.split'):
            raw = join_split(raw)
        return Image.open(io.BytesIO(raw)).convert('RGBA')
    raise KeyError(path)


stage_runtime()
with zipfile.ZipFile(args.archive) as archive, args.archive.open('rb') as stream:
    shelf = extract(archive, stream, 'Things/Building/Furniture/Shelf_south.psd.dds')
    steel = extract(archive, stream, 'Things/Item/Resource/Steel/Steel_c.psd.dds')
    silver = extract(archive, stream, 'Things/Item/Resource/Silver/Silver_c.psd.dds')
    gold = extract(archive, stream, 'Things/Item/Resource/Gold/Gold_c.psd.dds')
    floor_tex = extract(archive, stream, 'Terrain/Surfaces/WoodFloor.psd.dds')


def tint(image, colour):
    alpha = image.getchannel('A')
    out = ImageChops.multiply(image, Image.new('RGBA', image.size, colour + (255,)))
    out.putalpha(alpha)
    return out


# Vanilla shelf is stuff-tinted; use the game's WoodLog stuff colour. Floor uses the same blue-grey wash as earlier pages.
shelf = tint(shelf, (133, 97, 67))


def font(size):
    return ImageFont.truetype(str(args.font), size)


def pct_font(size):
    return ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf', round(size * .9))


def label(text):
    # RimWordFont draws capital O as an emblem; its lowercase o is the plain letter.
    return text.upper().replace('O', 'o')


def text(draw, x, y, value, size, fill, anchor='mt', width=None):
    value = label(value)
    while width and draw.textlength(value, font=font(size)) > width:
        size -= 2
    draw.text((x, y), value, font=font(size), fill=fill, anchor=anchor)
    return size


def percent(draw, x, y, number, size, fill):
    # RimWordFont has no % glyph; draw it in a fallback font, centred as one word.
    n = label(number)
    w = draw.textlength(n, font=font(size)) + draw.textlength('%', font=pct_font(size))
    left = x - w / 2
    draw.text((left, y), n, font=font(size), fill=fill, anchor='lt')
    draw.text((left + draw.textlength(n, font=font(size)), y + size * .03), '%', font=pct_font(size), fill=fill, anchor='lt')


def stripe(img, y, h):
    d = ImageDraw.Draw(img)
    w = img.width / len(STRIPE)
    for i, c in enumerate(STRIPE):
        d.rectangle((round(i * w), y, round((i + 1) * w), y + h), fill=c)


def floor(img, box, scale):
    tile = tint(floor_tex.resize((round(floor_tex.width * scale), round(floor_tex.height * scale)), Image.NEAREST), (120, 140, 160))
    x0, y0, x1, y1 = box
    for y in range(y0, y1, tile.height):
        for x in range(x0, x1, tile.width):
            img.alpha_composite(tile.crop((0, 0, min(tile.width, x1 - x), min(tile.height, y1 - y))), (x, y))


def paste(img, sprite, cx, cy, size):
    s = sprite.resize((size, round(sprite.height * size / sprite.width)), Image.NEAREST)
    img.alpha_composite(s, (round(cx - s.width / 2), round(cy - s.height / 2)))


def scene(img, top, unit):
    """Floor strip: a loose stack on the left, a shelf of goods on the right, with their wealth shares."""
    floor(img, (0, top, img.width, img.height), unit / 64)
    stripe(img, top, max(3, unit // 8))
    d = ImageDraw.Draw(img)
    h = img.height - top
    cy = top + h * .5
    left, right = img.width * .17, img.width * .64
    paste(img, steel, left - unit * .55, cy, round(unit * 1.1))
    paste(img, silver, left + unit * .55, cy, round(unit * 1.1))
    # Shelf_south is a 2x1 building drawn at drawSize (3,2) in vanilla; scale by width to keep proportions.
    shelf_w = round(unit * 4.4)
    paste(img, shelf, right, cy, shelf_w)
    for i, item in enumerate((steel, silver)):
        paste(img, item, right - unit * .55 + i * unit * 1.1, cy - unit * .05, round(unit * 1.0))
    return d, cy, left, right


def badge(img, cx, cy, value, size, fill):
    d = ImageDraw.Draw(img)
    r = size * 1.25
    d.rounded_rectangle((cx - r * 1.35, cy - r * .62, cx + r * 1.35, cy + r * .62), radius=r * .5, fill=NAVY + (235,), outline=fill, width=max(2, size // 14))
    percent(d, cx, cy - size * .48, value, size, fill)


# About/Preview.png is rendered by scripts/render-preview.py from the ASF artwork.

# ---- Gallery, 1920x1080: one idea, two numbers -------------------------------------------------------------------------
out = ROOT / 'docs' / 'images' / 'workshop'
out.mkdir(parents=True, exist_ok=True)
g = Image.new('RGBA', (1920, 1080), NAVY + (255,))
d = ImageDraw.Draw(g)
text(d, 960, 90, 'Stored goods count for less wealth', 84, CREAM, width=1700)
top, bottom = 235, 675
floor(g, (0, top, 1920, bottom), 3.0)
# Plain gold dividers, matching the thumbnail's band trim.
d.rectangle((0, top, 1920, top + 8), fill=GOLD)
d.rectangle((0, bottom - 8, 1920, bottom), fill=GOLD)
mid = (top + bottom) // 2 + 6
# Same goods both sides: Steel and Gold, loose on the floor, then on a vanilla shelf.
paste(g, steel, 380, mid, 200)
paste(g, gold, 600, mid, 200)
paste(g, shelf, 1430, mid, 560)
paste(g, steel, 1325, mid - 6, 180)
paste(g, gold, 1535, mid - 6, 180)
# Arrow between them.
ax, ay = 960, mid
d.polygon([(ax - 90, ay - 22), (ax + 30, ay - 22), (ax + 30, ay - 62), (ax + 110, ay), (ax + 30, ay + 62), (ax + 30, ay + 22), (ax - 90, ay + 22)],
          fill=CREAM)
percent(d, 490, 735, '100', 150, CREAM)
percent(d, 1430, 735, '50', 150, GOLD)
# Settings slider under the stored value: knob at the 50% default, labelled as adjustable.
sx0, sx1, sy = 1240, 1620, 935
knob = sx0 + (sx1 - sx0) * .5
d.rounded_rectangle((sx0, sy - 9, sx1, sy + 9), radius=9, fill=(58, 76, 92))
d.rounded_rectangle((sx0, sy - 9, knob, sy + 9), radius=9, fill=GOLD)
d.ellipse((knob - 26, sy - 26, knob + 26, sy + 26), fill=CREAM, outline=GOLD, width=5)
text(d, 1430, 985, 'Adjustable in Mod options', 44, CREAM, width=560)
g.convert('RGB').save(out / 'wealth-comparison.png', optimize=True)
print('Rendered docs/images/workshop/wealth-comparison.png')
