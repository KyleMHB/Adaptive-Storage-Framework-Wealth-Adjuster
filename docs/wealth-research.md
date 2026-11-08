# Wealth accounting investigation

Investigated: 2026-11-08. Static inspection only; no game session was launched and no files outside this workspace were modified.

## Sources and method

Read installed method bodies with the existing [ildump tool](</E:/Coding/Rimworld/Lumen Stones/tools/ildump/Program.cs>) and its prebuilt Release executable. No new probes or test infrastructure were written.

- [Game version](</E:/Steam/steamapps/common/RimWorld/Version.txt>): 1.7.5000 rev1275.
- [Game assembly](</E:/Steam/steamapps/common/RimWorld/Assembly-CSharp.dll>): `RimWorld.WealthWatcher.CalculateWealthItems`, `WealthItemsFilter`, `ForceRecount`, `RecountIfNeeded`, `get_WealthItems`, `get_WealthTotal`; `Verse.Map.get_PlayerWealthForStoryteller`; `Verse.Mod.WriteSettings`.
- [Prepare for Combat assembly](</E:/Steam/steamapps/workshop/content/294100/3039568123/1.7/Assemblies/PrepareForCombat.dll>): `PrepareForCombat.Map_PlayerWealthForStoryteller.Prefix`.
- [Saved PFC settings](</C:/Users/Kyle/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/Config/Mod_3039568123_PrepareForCombatMod.xml>) and [active mods](</C:/Users/Kyle/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/Config/ModsConfig.xml>).
- [Installed ASF assembly](</E:/Steam/steamapps/workshop/content/294100/3033901359/1.6/Assemblies/AdaptiveStorageFramework.dll>): public visibility and method body of `AdaptiveStorage.Utility.ThingExtensions.StoringAdaptiveStorage`; public `AdaptiveStorage.ThingClass`, `ContainsAndAllows`, and `StoredThings`.
- [Local 1.7 build example](</E:/Coding/Rimworld/Lumen Stones/Source/LumenStones.csproj>): `net9.0`, explicit game assembly references, Harmony reference, dependency copying disabled. This is an example for the installed runtime, not an instruction to copy every reference.
- [Harmony transpiler documentation](https://harmony.pardeike.net/v2/articles/patching-transpiler.html): recommends narrowly changing calls/instructions and explains that transpilers are chained. No ordering guarantee against arbitrary other wealth replacements is inferred.

## Findings

1. `CalculateWealthItems()` clears its temporary list, recursively gathers map items using `WealthItemsFilter`, then includes only things with `SpawnedOrAnyParentSpawned` and non-fogged `PositionHeld`. The contribution is exactly one `Thing.get_MarketValue()` call multiplied by `stackCount` and added to the running total. It clears the temporary list before returning. Getter call offset in this build: IL_0078; multiplication IL_008f. These offsets document the evidence and must not be patch anchors.
2. `WealthItemsFilter` excludes passing ships, map components, non-player pawn holders, and quest-lodger pawn holders. Preserving the original enumeration avoids recreating these rules incorrectly.
3. `ForceRecount()` separately computes item, building, floor, and pawn wealth. It also recounts and adds attached pocket-map wealth. A per-item change inside `CalculateWealthItems()` leaves those other categories and aggregation rules intact.
4. `get_WealthItems()` and `get_WealthTotal()` call `RecountIfNeeded()`. During play this method recounts only when elapsed ticks are greater than 5,000. There is no need to intercept storage lifecycle events for the requested next-recount behavior.
5. The installed PFC prefix multiplies `WealthItems` by its saved `PercentWealthItems` on home maps under normal wealth difficulty, then adds separately weighted building wealth and pawn wealth. The saved item factor is 0.400000006. Thus a storage factor of 0.5 and this PFC setting yield approximately 0.2 for the affected storyteller item term. This establishes static composition, not runtime compatibility with every mod in the active pack.
6. Both vanilla and PFC contain time-based fixed-wealth branches. Reducing reported wealth is not a promise of a particular threat-point or raid-strength result. PFC's non-home branch also bypasses the usual map item-wealth total.
7. ASF's inspected compiled lookup agrees with its shipped source: spawned item lookup is based on the slot-group parent at its position; it does not require `ContainsAndAllows`. The method is public and can be called directly. Require the item itself to be spawned for the chosen physical-cell semantics, avoiding its alternate owner-based path for packed storage.
8. ASF's installed About metadata advertises 1.4–1.6, not 1.7, and the package contains versioned 1.4, 1.5, and 1.6 assemblies. A new mod can target 1.7, but it must runtime-check the selected ASF assembly before claiming support. Advertising 1.7 in Progression's metadata does not establish ASF runtime compatibility.

## Recommended seam

Replace only the market-value call in the item accumulation with a static `Thing -> float` helper that reads the ordinary market value and applies the configured factor for qualifying storage. Leave all eligibility, stack multiplication, cleanup, and aggregation with vanilla. Use direct ASF references; avoid a second map scan, stat modifiers, item mutation, and persistent membership state.

This is an implementation recommendation based on the inspected bodies, not a tested patch. No Harmony patch has been installed, and the active modpack's runtime patch list has not been audited.
