# Testing

Developer harness: `DevTests/` (batch `sfwa-wealth`, cases in `DevTests/Wealth/WealthBatch.cs`, expected values from `docs/spec.md`). It uses the shared `../RimWorldDevTests` runner.

```powershell
dotnet build Source/StorageFrameworkWealthAdjuster.csproj -c Release
dotnet build DevTests/StorageFrameworkWealthAdjuster.DevTests.csproj -c Release
python tools/sfwa_test.py run sfwa-wealth --seed 1
python tools/sfwa_test.py close-session
python tools/sfwa_test.py release-check
```

The runner uses the active `ModsConfig.xml`. Back it up, set the test mod list, and restore it afterwards. The installed mod folder must match the working tree (`About`, `Assemblies`, `Languages`).

## Result recording

### 2026-11-08 - First harness run

- Version/environment: RimWorld 1.7.5000 rev1275, seed 1, no DLC. Mods: Harmony 3.0.1.0, Core, Vanilla Expanded Framework, Adaptive Storage Framework (1.6 assembly via fallback), VFE Props and Decor, Reel's Expanded Storage, this mod 1.0.0. The user's 1,535-entry `ModsConfig.xml` was backed up, temporarily replaced, then restored (SHA-256 matched).
- Run: `20261108-184643-sfwa-wealth-s1-d5895993`. 11 pass, 0 fail, 0 skip. No mod errors in Player.log; `release-check` passed after `close-session`.
- Observed (75 Steel, unit value 1.9, ASF crate `ReelStorageNewWoodCrate`): floor 142.5; in crate 71.25; split 75 in/25 out 118.75; filter-rejected in crate 71.25; carried by a colonist standing on the crate 142.5; vanilla stockpile 142.5; settings 0%/25%/100% gave 0/35.625/142.5; stored unit market value 1.9; crate market value and building wealth unchanged by contents; after save/reload 71.25. The transpiler was the only patch on `CalculateWealthItems` in this set.
- Limits: all cases are mechanical (developer spawns, forced recounts). Not covered: the normal 5,000-tick recount, trade-dialog prices, settings persistence across a game restart, Prepare for Combat interaction, packed/minified storage, and the full modpack (other mods could also patch the wealth method).

### 2026-11-08 - Full modpack run and storage survey

- Environment: RimWorld 1.7.5000 rev1275, seed 1, the user's 1,531-mod list with this mod added (Load Them Last moved `sk.noambient` and `vr.missilegirl` after it). `ModsConfig.xml` was restored afterwards (SHA-256 matched the backup).
- Run: `20261108-185147-run-all-s1-fe2cdeee`. 13/13 gameplay cases pass; 13/13 runner diagnostics as intended. No `[Storage Framework Wealth Adjuster]` errors in Player.log; `release-check` passed. Earlier small-set survey run: `20261108-184931-sfwa-storage-survey-s1-253b136e`.
- `sfwa-wealth`: identical values to the 7-mod run; our transpiler was the only patch on `CalculateWealthItems` in the full pack.
- `sfwa-storage-survey`: 333 loaded storage defs. All 289 ASF defs measured 0.5 at the default setting. 43 of 44 non-ASF defs measured 1.0; `LTS_CompactNutrientPasteGrinder` (More Gravship Workbenches) threw a NullReferenceException when spawned directly, so it was not measured. ASF converts vanilla Shelf, ShelfSmall, Dresser and ToolCabinet, so those are discounted.
- Non-ASF storage (counts in full): Hopper (Core); AB_PropaneDrain/Tap (Alpha Biomes); AG_AncientShelf (Alpha Genes); Aqua_Hopper (Aquariums!); BMT_DisplayCase (Biomes! Fossils); DankPyon_CultShelf (Medieval Overhaul); Dogbowl (MyDogsHouse); ED_PetBowl (Erin's Decorations); MP_MilkDrain/Tap (Milk Pipe Network); MUS_SpaceBase_Shelf/ShelfSmall (MUS Space Base Furniture); MedievalVendingMachine (Hospitality: Vending machines); OWD_DimensionalStorage (One with Death); PFK_ShelfSmall, PFS_ShelfSmall (Ponpeco Furnitures); PipedARC_UraniumHopper (Uranium Buffer Pipe Network); Reel_AmmoDrain/Output (Reel's Turret Pipeline); Shelf_RepairRack (VFE Spacer); Shelf_WeaponRack (VFE); USH_LuciferiumPrecisionBooster, VCHE_CompressedScarletSludgePlatform, VCHE_ScarletMechanitesPlatform (Ushankas Luciferium Expansion); VCHE_ChemfuelDrain/Tap, VCHE_DeepchemDrain/Tap (Vanilla Chemfuel Expanded); VFEFactory_FactoryHopper (VFE Factory); VFEM2_AilmentsShelf, VFEM2_AutofillShelf (VFE Medieval 2); VGE_AstrofuelDrain/Tap, VGE_GravshipShelf, VGE_OxygenCanisterEmptier/Filler (VGE Chapter 1); VNPE_NutrientPasteFeeder (VNPE); VREA_NeutroDrain/Tap (VRE Android); VRE_HemogenDrain/Tap (VRE Sanguophage); VRecyclingE_WasteCrate (Vanilla Recycling Expanded); LTS_CompactNutrientPasteGrinder (unmeasured).
- Limits: survey buildings were spawned directly (no designator, research, power or construction); readings are forced recounts. Not covered: normal 5,000-tick recount, trade-dialog prices, settings persistence across restart, Prepare for Combat's storyteller multiplier, packed storage.
- Runner note: the first full-pack attempt (`20261108-185030-run-all-s1-f1014d14`) aborted because Load Them Last reordered the list and restarted the game during mod construction; the launcher lost the replacement process. Rerunning with the reordered list worked.
