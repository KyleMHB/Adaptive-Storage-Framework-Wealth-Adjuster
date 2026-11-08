# Adaptive Storage Framework Wealth Adjuster

A RimWorld patch for [Adaptive Storage Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3033901359) (ASF). Items physically in ASF storage buildings count for less colony wealth: 50% by default, adjustable from 0 to 100%. Market value and trade prices stay the same.

## Features

- Applies to every ASF storage building from any mod, including vanilla shelves that a storage mod converts to ASF.
- Changes only the colony item wealth total. Market value, trade prices and the storage building's own wealth are unchanged.
- Split stacks are counted separately, and items stay discounted while in storage even when the filter rejects them.
- Stockpile zones, non-ASF storage, carried items and packed storage count in full.
- Saves nothing in your save file, so it can be added or removed at any time. Wealth updates at the game's next recount.

## Installation

Subscribe on the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3836261869), or copy `About`, `Assemblies`, `Languages` and `LICENSE` into a folder under `RimWorld/Mods`. Requires [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) and Adaptive Storage Framework, plus a mod that adds ASF storage buildings. Load it after Harmony and ASF. Supports RimWorld 1.7.

## Configuration

Options, then Mod options, then Adaptive Storage Framework Wealth Adjuster. The *Stored item wealth counted* slider sets the percentage. It applies to every save and takes effect at the next wealth recount.

## Building from source

Requires the .NET SDK and a local RimWorld 1.7 install with Harmony and ASF from the Workshop. Reference paths default to `E:\Steam\...`; override `RimWorldPath`, `HarmonyPath` and `AsfPath` for another layout.

```powershell
dotnet build Source/StorageFrameworkWealthAdjuster.csproj -c Release
```

The DLL is written to `Assemblies/`. Developer tests in `DevTests/` use the sibling `RimWorldDevTests` harness; see [TESTING.md](TESTING.md). Workshop artwork scripts live in `scripts/`.

## Credits

The Workshop thumbnail uses Adaptive Storage Framework's Workshop artwork, since this is a patch for that framework.

## License

Released under the **GNU General Public License v3.0**. See [`LICENSE`](LICENSE) for the complete terms.

Commercial use is allowed. Distributed modified versions must remain under GPLv3 and make corresponding source available as required by the license.

## AI?

Do I use AI to build my mods? **Yes.**  
Am I obsessive about how I use it and the quality of the results? **Also yes.**

I understand the scepticism.
I've seen the flood of AI-generated mods that look like someone typed a prompt, uploaded whatever came out, and called it done.

That bothers me too.

My workflow involves a lot of iteration, review, and testing. The same care I'd put into building everything by hand. I've spent hundreds of hours building mods. Using AI doesn't lower the standard I expect from my work.

## Links

Support me on Ko-fi.

[![Support me on Ko-fi](https://img.shields.io/badge/Support_me_on_Ko--fi-72a4f2?style=for-the-badge&logo=kofi&logoColor=white)](https://ko-fi.com/I7L525WMJ6)
[![GitHub Repository](https://img.shields.io/badge/GitHub-Repository-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/KyleMHB/Adaptive-Storage-Framework-Wealth-Adjuster)
