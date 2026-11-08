# ASF storage membership research

Investigated 2026-11-08 against the installed RimWorld `1.7.5000 rev1275` and local Workshop sources/assemblies. Static inspection only; no game launch, code changes, or tests.

## Recommended eligibility rule

The user selected all ASF buildings, a configurable percentage counted with a 50% default, RimWorld 1.7 only, and physical storage membership including rejected/excess items. For each item already admitted by the vanilla wealth calculation, require `item.Spawned`, obtain `item.StoringAdaptiveStorage()`, and apply the factor when the result is a spawned `AdaptiveStorage.ThingClass`. Do **not** require `ContainsAndAllows`. This covers ASF building subclasses without naming Progression buildings or requiring Progression as a dependency. Requiring a directly spawned item and spawned storage excludes carried items, nested-container contents, and packed/minified storage from the discount; vanilla inclusion rules remain authoritative for whether they contribute ordinary wealth.

The lookup alone does **not** establish valid storage. For spawned items it uses `map.haulDestinationManager.SlotGroupParentAt(item.Position) as ThingClass`; for unspawned items it can return the `holdingOwner.Owner as ThingClass`. Therefore an item sitting in a storage cell can be found even after filters reject it. The installed 1.6 framework DLL confirms the source implementation. [Lookup source](/E:/Steam/steamapps/workshop/content/294100/3033901359/Source/AdaptiveStorage/Utility/ThingExtensions.cs:18), [installed DLL](/E:/Steam/steamapps/workshop/content/294100/3033901359/1.6/Assemblies/AdaptiveStorageFramework.dll).

`ContainsAndAllows` checks ASF's `_validStoredThings` set. `TryAddStoredCellWiseThing` and `UpdateValidStoredItemsAt` populate it only for items passing the storage's fixed/user filters and within its per-cell stack limit. ASF itself uses this predicate for stacking destinations, but it would incorrectly exclude rejected or excess stacks under the user's physical-membership requirement. [Membership and capacity source](/E:/Steam/steamapps/workshop/content/294100/3033901359/Source/AdaptiveStorage/ThingCollection.cs:276), [membership contract](/E:/Steam/steamapps/workshop/content/294100/3033901359/Source/AdaptiveStorage/ThingCollection.cs:445), [filter source](/E:/Steam/steamapps/workshop/content/294100/3033901359/Source/AdaptiveStorage/ThingClass.cs:399), [existing use](/E:/Steam/steamapps/workshop/content/294100/3033901359/Source/AdaptiveStorage/HarmonyPatches/StoreUtilityPatches.cs:157).

`ThingClass` extends `Building_Storage`. The lookup casts with `as ThingClass` (confirmed as IL `isinst AdaptiveStorage.ThingClass` in the installed DLL), so subclasses qualify. A generic ASF integration can use this API directly; it needs no def-name list, def-owner/package test, research-tier inference, or XML extension for the agreed single global percentage. [Building API](/E:/Steam/steamapps/workshop/content/294100/3033901359/Source/AdaptiveStorage/ThingClass.cs:15), [lookup](/E:/Steam/steamapps/workshop/content/294100/3033901359/Source/AdaptiveStorage/Utility/ThingExtensions.cs:18).

## Progression compatibility

Progression primarily patches definitions owned by other mods and vanilla. Its root patches add research prerequisites to `Shelf`, `ShelfSmall`, `Bookcase`, and `BookcaseSmall`; optional folders patch, among others, Reel Storage crates/feeders/fridges and vanilla Odyssey outfit stands. Filtering by `ferny.progressionstorage` as the owning package would therefore miss buildings that Progression modifies. Generic ASF type eligibility avoids that problem and does not discount non-ASF storage merely because Progression adjusts its research. [Root patches](/E:/Steam/steamapps/workshop/content/294100/3292746186/Patches/patch.xml), [Reel patches](</E:/Steam/steamapps/workshop/content/294100/3292746186/1.6/Mods and Shit/Reel Storage/Patches/reel storage general patch.xml>), [Odyssey patches](</E:/Steam/steamapps/workshop/content/294100/3292746186/1.6/Mods and Shit/Odyssey/Patches/odyssey general patch.xml>).

Progression advertises 1.7 but defines only `v1.5` and `v1.6` load-folder entries. This is **not** a vanilla 1.7 content-loading blocker: installed `Verse.ModContentPack.InitLoadFolders()` first requests the current version, then chooses the latest declared version not newer than the current game. Its fallback filter is `<>c.<InitLoadFolders>b__58_1`. For Progression's entries, that selects `v1.6`, including root and conditional folders. ASF has no `LoadFolders.xml` and versioned assembly folders through 1.6; the same method's directory fallback chooses its 1.6 folder on 1.7. [Progression mapping](/E:/Steam/steamapps/workshop/content/294100/3292746186/LoadFolders.xml), [game method evidence](/E:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll), [installed version](/E:/Steam/steamapps/common/RimWorld/Version.txt).

Method evidence was read without compiling or executing game code using the existing `E:/Coding/Rimworld/Lumen Stones/tools/ildump/bin/Debug/net9.0/ildump.dll`, targeting `ModContentPack InitLoadFolders`, `Verse.ModContentPack/<>c <InitLoadFolders>b__58_1`, `ModMetaData LoadFoldersForVersion`, and the installed ASF `ThingExtensions StoringAdaptiveStorage` and `ThingCollection ContainsAndAllows` methods.

## Focused runtime acceptance cases

- A valid stack inside any ASF building receives the configured wealth factor; a plain stockpile or non-ASF building does not.
- A rejected stack at that ASF building's cell receives the same factor; changing storage filters does not change the discount while the item remains physically there.
- An excess stack beyond ASF cell capacity receives the same factor while physically at that storage cell.
- Moving part of a stack outside storage changes only that portion; ordinary market value and trade prices remain unchanged.
- Save/reload reconstructs eligibility from ASF; no item-specific state is saved by this mod.
- Packing/minifying storage restores ordinary wealth for packed contents whenever vanilla counts those contents; carried items and nested-container contents also retain ordinary wealth.

No runtime compatibility claim is established by this research. In particular, active modpack patches may alter load-folder selection or membership update timing.
