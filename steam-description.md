Your stockpiled goods count for less colony wealth while they sit in Adaptive Storage Framework (ASF) storage.

Every stack in your warehouse adds to colony wealth, and wealth feeds the storyteller. With this mod, items in ASF buildings count at a percentage you choose, 50% by default. Their market value stays the same, so you still trade them at full price.

It's one more reason to build a proper storage room with shelves instead of leaving everything in a stockpile zone.

[b]Mod version:[/b] 1.0.0
[b]Supported game version:[/b] RimWorld 1.7

[h1]Features[/h1]
[list]
[*]Items physically in any ASF storage building count at the configured percentage toward colony item wealth.
[*]Works with every building made with ASF, from any mod. Vanilla shelves, dressers and tool cabinets count too when your mods convert them to ASF.
[*]Market value and trade prices are unchanged. The discount applies only when the game totals colony wealth.
[*]The storage building's own wealth is unchanged.
[*]Split stacks are counted separately, so only the part inside storage is discounted.
[*]Items stay discounted while they sit in storage, even after you change the storage filter to reject them.
[/list]

[h2]What does not get the discount[/h2]
[list]
[*]Items in stockpile zones or in storage that does not use ASF, such as vanilla hoppers.
[*]Items a pawn is carrying, even across a storage cell.
[*]Contents of packed or minified storage.
[/list]

[h1]How to use[/h1]
Add the mod to an existing or new game and store items in your ASF shelves, crates and cabinets as usual. There is nothing to build or research.

The game recounts colony wealth about every 83 seconds of running game time at normal speed. A stack you move into or out of storage changes your wealth at the next recount.

[h1]Settings[/h1]
Open Options, then Mod options, then Adaptive Storage Framework Wealth Adjuster. The [i]Stored item wealth counted[/i] slider goes from 0% to 100%:
[list]
[*]0% means stored items add no item wealth.
[*]50% is the default.
[*]100% turns the discount off.
[/list]
The setting applies to every save and takes effect at the next wealth recount.

[h1]Requirements[/h1]
[list]
[*][url=https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077]Harmony[/url]
[*][url=https://steamcommunity.com/sharedfiles/filedetails/?id=3033901359]Adaptive Storage Framework[/url]
[/list]
You also need at least one mod that adds ASF storage buildings, such as Reel's Expanded Storage. Load this mod after Harmony and Adaptive Storage Framework.

[h1]Compatibility and save safety[/h1]
[list]
[*]Safe to add to an existing save. Discounts appear at the next wealth recount.
[*]Safe to remove. The mod saves nothing in your save file, so wealth returns to normal at the next recount. Past entries in the wealth history graph stay as they were.
[*]Prepare for Combat applies its own item wealth multiplier on top of this one. With this mod at 50% and Prepare for Combat at 40%, stored items count for about 20% of their value toward the storyteller's item wealth.
[*]Lower wealth does not reduce raid strength by the same amount. With a wealth-independent difficulty setting, the storyteller ignores wealth altogether.
[*]Combat Extended is not affected, since this mod does not touch combat.
[*]A mod that replaces the vanilla item wealth calculation entirely would conflict. The mod logs one error at startup if it cannot find what it patches, and wealth then counts normally.
[/list]

[h1]Credits[/h1]
The thumbnail uses the [url=https://steamcommunity.com/sharedfiles/filedetails/?id=3033901359]Adaptive Storage Framework[/url] Workshop artwork, since this is a patch for that framework.

[h1]License[/h1]
Released under the GNU General Public License v3.0. Commercial use is allowed. Distributed modified versions must remain under GPLv3 and provide corresponding source as the license requires.

[h1]AI?[/h1]

Do I use AI to build my mods? [b]Yes.[/b]
Am I obsessive about how I use it and the quality of the results? [b]Also yes.[/b]

I understand the scepticism.
I've seen the flood of AI-generated mods that look like someone typed a prompt, uploaded whatever came out, and called it done.

That bothers me too.

My workflow involves a lot of iteration, review, and testing. The same care I'd put into building everything by hand. I've spent hundreds of hours building mods. Using AI doesn't lower the standard I expect from my work.

[h1]Links[/h1]
Support me on Ko-fi.

[url=https://ko-fi.com/I7L525WMJ6][img]https://img.shields.io/badge/Support_me_on_Ko--fi-72a4f2?style=for-the-badge&logo=kofi&logoColor=white[/img][/url]
[url=https://github.com/KyleMHB/Adaptive-Storage-Framework-Wealth-Adjuster][img]https://img.shields.io/badge/GitHub-Repository-181717?style=for-the-badge&logo=github&logoColor=white[/img][/url]

To report a problem, post in the comments with your Player.log file and your mod list.
