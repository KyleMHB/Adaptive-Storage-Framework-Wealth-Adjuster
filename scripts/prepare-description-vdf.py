"""Write a description-only SteamCMD VDF for this mod's Workshop item.

Reads the item ID from About/PublishedFileId.txt (written by RimWorld's in-game uploader) in the installed mod
or the project, and the description from steam-description.md. No network calls or credentials.

    python scripts/prepare-description-vdf.py
"""
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
INSTALLED = Path(r'E:\Steam\steamapps\common\RimWorld\Mods\Adaptive Storage Framework Wealth Adjuster')
OUT = ROOT / 'artifacts' / 'publication-2026-11-08' / 'description-upload.vdf'


def quote_vdf(text):
    # Same escaping as steam-workshop-utils/scripts/prepare_workshop_update.py.
    if any(ord(c) < 32 and c not in '\n\t' for c in text):
        raise ValueError('description contains unsupported control characters')
    return '"' + text.replace('\\', '\\\\').replace('"', '\\"') + '"'


ids = {p.read_text(encoding='utf-8-sig').strip() for p in (INSTALLED / 'About' / 'PublishedFileId.txt', ROOT / 'About' / 'PublishedFileId.txt') if p.is_file()}
if len(ids) != 1 or not next(iter(ids)).isdigit() or next(iter(ids)) == '0':
    sys.exit(f'need exactly one nonzero Workshop ID in About/PublishedFileId.txt, found {sorted(ids) or "none"}')
item = ids.pop()
description = (ROOT / 'steam-description.md').read_text(encoding='utf-8').strip()
OUT.parent.mkdir(parents=True, exist_ok=True)
OUT.write_text('"workshopitem"\n{\n'
               f'    "appid" "294100"\n    "publishedfileid" "{item}"\n    "description" {quote_vdf(description)}\n'
               '}\n', encoding='utf-8')
print(f'wrote {OUT} for item {item}')
