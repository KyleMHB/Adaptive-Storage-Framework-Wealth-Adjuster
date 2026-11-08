# Adaptive Storage Framework Wealth Adjuster

Date: 2026-11-08
Status: Implemented 2026-11-08. Mechanical in-game checks passed in a 7-mod set (see TESTING.md); modpack and normal-recount checks remain.

## Purpose and agreed scope

Create a separate RimWorld mod in this workspace that reduces the colony item wealth contributed by items physically stored in Adaptive Storage Framework (ASF) buildings. Ordinary market value, trade prices, and the storage building's own wealth remain unchanged.

Decisions confirmed by the user:

- Cover all ASF storage buildings, including subclasses, regardless of the content mod that defines them. Progression: Storage is an example, not a dependency or selection boundary.
- Provide one global configurable percentage: how much stored item wealth is counted.
- Default to 50% counted, equivalent to a 50% reduction.
- Count items physically in the storage even when filters reject them or they await hauling.
- Ship as a separate mod named Adaptive Storage Framework Wealth Adjuster (renamed 2026-11-08 from Storage Framework Wealth Adjuster; packageId, assembly and folder names unchanged).
- Target RimWorld 1.7 initially.

## Required behavior

For each stack already included by the game's item-wealth calculation:

`contribution = ordinary unit MarketValue * stackCount * factor`

`factor = configured percentage / 100` for qualifying storage; otherwise `factor = 1`.

Qualification means the item is spawned directly in a storage cell whose slot-group parent is an `AdaptiveStorage.ThingClass` (including derived classes), and that building is spawned on the same map. Use ASF's `StoringAdaptiveStorage()` lookup after checking that the item is spawned. Do not require `ContainsAndAllows()`, storage-filter acceptance, capacity availability, power, or player ownership. Preserve vanilla's own wealth eligibility; do not introduce faction exclusions.

This physical-cell rule deliberately includes rejected and excess stacks still occupying storage cells. It excludes a pawn's inventory while walking over storage, items hauled away, contents held inside a separate container, and contents of packed/minified/despawned storage buildings. Such items retain their ordinary contribution if vanilla counts them. Vanilla-excluded things remain excluded.

- Moving a stack in or out changes its contribution on the next normal wealth recount. Do not force a recount on every haul, stack change, or settings slider movement.
- Split stacks are evaluated individually, using each stack's current position and count.
- Apply the factor once to each eligible stack. Never multiply the final colony total, alter building/pawn wealth, or patch a global stat getter.
- Do not mutate items, defs, storage settings, or save data. Derive membership during each recount.
- Save/reload and mod removal require no migration. Removing the mod restores ordinary accounting on a fresh recount; past wealth-history samples are not rewritten.
- The configured factor affects reported item wealth and consumers of that wealth. It does not promise a proportional reduction in raid strength.

## Settings

Use RimWorld's normal Mod Settings screen and `ModSettings` persistence. Settings apply globally across saves.

| Control | Requirement |
| --- | --- |
| Label | Stored item wealth counted |
| Range | 0–100%, whole percentage points |
| Default | 50% |
| Help text | Items physically in ASF storage count at this percentage toward colony item wealth. Market value and trade prices are unchanged. Changes apply on the next wealth recount. |
| 0% | Eligible stored contents contribute zero item wealth |
| 100% | Ordinary item wealth; return the original value without a storage lookup |

Persist an integer percentage with a default of 50, clamp loaded values to 0–100, and derive the floating-point multiplier when needed. Use a slider with its current percentage displayed. Use keyed English strings for the settings UI. No per-building controls, tier tables, presets, enable switch, or custom settings framework are needed.

## Implementation design

Use one Harmony transpiler on private `RimWorld.WealthWatcher.CalculateWealthItems()`. Replace the specific `Thing.MarketValue` getter call used for the item contribution with a static helper accepting the same `Thing` and returning the adjusted unit value.

The helper reads the ordinary `thing.MarketValue` once, then applies the storage factor when qualified. This leaves ordinary market-value patches from other mods effective and preserves the existing `stackCount` multiplication. It does not intercept stat queries elsewhere.

Keep vanilla's recursive enumeration, holder filter, spawned-parent test, fog test, accumulation, and temporary-list cleanup intact. Do not replace the method with a prefix, duplicate its enumeration in a postfix, patch `ForceRecount`, or implement an independent item cache. This also leaves vanilla pocket-map aggregation intact.

Match the getter by its method identity, not by IL offsets or local-variable indices. Inspect the incoming instructions before mutating them: the verified method contains one matching getter. If the target is missing or ambiguous, return the incoming instructions unchanged and emit one clear patch diagnostic for that patch attempt. Preserve instruction labels and exception blocks when replacing the call. Do not silently apply a partial discount or invent an alternate calculation.

Harmony chains transpilers; this narrow replacement follows its recommendation to change as little as possible. Other mods that replace the entire wealth method or replace the same call remain a compatibility risk, to be checked during validation rather than addressed through a second accounting path. [Harmony transpiler documentation](https://harmony.pardeike.net/v2/articles/patching-transpiler.html).

Use direct references to ASF's public lookup and `ThingClass`. Declare Harmony (`brrainz.harmony`) and ASF (`adaptive.storage.framework`) as required dependencies and load after them. Do not bundle their DLLs. Progression and Vanilla Expanded Framework are not dependencies of this mod.

Build against the installed 1.7 assemblies and the ASF assembly selected for that game. The installed ASF package currently supplies its 1.6 assembly under the game's version fallback. Use configurable local reference paths, with referenced game/framework DLLs excluded from output. The neighboring Lumen Stones 1.7 project is a verified local build-layout example; do not copy its entire reference list without checking what this mod needs. The currently installed game is 1.7.5000 rev1275. Initial metadata advertises 1.7 only.

Keep the code limited to mod/settings registration, the settings UI, and the wealth patch/helper. No per-map component, item component, XML extension, event subscription, new production library, or test framework is required.

## Existing modifiers and timing

The installed game's `WealthItems` and `WealthTotal` accessors call `RecountIfNeeded()`, which recounts during play when more than 5,000 game ticks have elapsed since the previous count. At normal simulation speed that is about 83 seconds of running simulation, and the refresh is access-driven. A manual `ForceRecount()` can be used for validation. This mod retains that behavior rather than promising an immediate update.

Prepare for Combat's installed 1.7 patch reads `WealthItems` and subsequently multiplies it by `PercentWealthItems` on player-home maps in normal wealth mode. Its current saved item factor is approximately 0.4. At this mod's default 0.5 factor, eligible stored items therefore contribute approximately 0.2 of their ordinary value to that item term. Do not compensate for or bypass this modifier.

In wealth-independent difficulty, the storyteller uses time-based wealth; reduced reported item wealth need not reduce threat points. Prepare for Combat also takes a different path for non-home maps. These behaviors are outside this mod's scope.

Evidence: [wealth investigation](wealth-research.md) and [storage investigation](storage-research.md). No runtime compatibility claim has been established yet.

## Acceptance and verification

For implementation, first perform a clean build and static checks of the Harmony target, its unique getter call, dependency metadata, and output contents. Then use a focused in-game scenario on 1.7 with Harmony, ASF, one actual ASF storage building, and this mod. Record the game/framework versions, active mods, and patch diagnostics. Use forced recounts to isolate arithmetic; separately check one normal recount transition. Compare controlled item-wealth deltas rather than total map wealth.

| Scenario | Expected result after recount |
| --- | --- |
| Known stack on the floor, then in ASF storage | Full contribution outside; 50% inside at the default setting |
| Same stack removed or carried by a pawn | Full contribution if otherwise counted by vanilla |
| Split between floor and ASF storage | Only the portion in ASF storage is discounted |
| Filters changed to reject the stored stack | Discount remains while the stack physically occupies that storage cell |
| Pawn carrying the stack over a storage cell | No storage discount |
| Same stack in vanilla storage | Full contribution |
| Change setting to 0%, 25%, and 100% | Respectively 0%, 25%, and 100% of stored stack value; settings persist after restart |
| Query item market value/trade quote inside and outside | Unchanged under equivalent trading conditions |
| Compare empty/full storage building's own wealth | Unchanged by this mod |
| Save/reload with a stored stack | Same configured contribution on a fresh recount, with no item mutations |
| Repeat arithmetic with Prepare for Combat enabled | Reported item wealth uses this mod's factor; normal home-map storyteller item term additionally uses PFC's configured multiplier |

Check startup/patch errors and the focused scenario with the active modpack before claiming compatibility with that pack. A passing build or static inspection alone is insufficient. If runtime validation cannot be performed, report it as unverified and retain the exact checks above for the maintainer. Do not create a new harness or broad compatibility matrix. Read the shared RimWorld Dev Tests instructions before authoring any future harness cases.

## Delivery boundary

This specification authorizes no implementation, deployment, Workshop publication, game launch, or mod-configuration changes by itself. The current task is investigation and specification. Implementation can proceed from this document when requested; no feature choices remain open.
