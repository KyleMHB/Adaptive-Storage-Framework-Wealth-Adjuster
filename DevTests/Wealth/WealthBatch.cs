using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveStorage;
using HarmonyLib;
using RimWorld;
using RimWorldDevTests;
using Verse;

namespace StorageFrameworkWealthAdjuster.DevTests;

// Expected values: docs/spec.md, "Acceptance and verification". Every case is a mechanical reading: the developer spawns
// stacks directly and forces a wealth recount, so no pawn job or normal 5,000-tick recount is exercised.
// The ASF building comes from Reel's Expanded Storage (ReelStorageNewWoodCrate, 1x1, Woody, 8 stacks per cell).
public class WealthBatch : Batch
{
    private const string CrateDef = "ReelStorageNewWoodCrate";
    private const int Stack = 75;
    private const float Tolerance = 0.05f;

    public override string Id => "sfwa-wealth";
    public override int SegmentCount => 2;
    public override int MapSize => 75;
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[]
    {
        "sfwa.patch", "sfwa.floor", "sfwa.stored", "sfwa.split", "sfwa.rejected", "sfwa.carried", "sfwa.stockpile",
        "sfwa.settings", "sfwa.market-value", "sfwa.building-wealth", "sfwa.reload",
    }.Select(id => new PlannedCase(id, Levels.Mechanical)).ToArray();

    private static float Items(Map map)
    {
        map.wealthWatcher.ForceRecount(false);
        return map.wealthWatcher.WealthItems;
    }

    private static Thing Steel(int count)
    {
        var t = ThingMaker.MakeThing(ThingDefOf.Steel);
        t.stackCount = count;
        return t;
    }

    private static Thing SpawnSteel(Map map, int count, IntVec3 cell) => GenSpawn.Spawn(Steel(count), cell, map);

    // Item-wealth delta caused by spawn(); every thing it returns is destroyed afterwards.
    private static float Delta(Map map, Func<IEnumerable<Thing>> spawn)
    {
        float before = Items(map);
        var things = spawn().ToList();
        float after = Items(map);
        foreach (var t in things) if (!t.Destroyed) t.Destroy();
        return after - before;
    }

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        var settings = WealthAdjusterMod.Settings;
        int original = settings.storedPercent;
        try
        {
            settings.storedPercent = 50;
            if (index == 0) Run(ctx);
            else Reloaded(ctx);
        }
        finally
        {
            settings.storedPercent = original;
        }
        if (index == 0 && ctx.Recall("crate") != null) yield return Wait.Reload();
    }

    private void Run(BatchContext ctx)
    {
        var map = ctx.Map;
        var method = AccessTools.Method(typeof(WealthWatcher), "CalculateWealthItems");
        var info = Harmony.GetPatchInfo(method);
        var transpilers = info?.Transpilers.Select(p => p.owner).ToArray() ?? Array.Empty<string>();
        var others = info == null ? "none" : string.Join(",", info.Prefixes.Concat(info.Postfixes).Concat(info.Finalizers).Concat(info.Transpilers).Select(p => p.owner).Distinct());
        ctx.Begin("sfwa.patch").Expect("transpilerOwner", "kylemhb.storageframeworkwealthadjuster")
            .Observe("transpilerOwners", string.Join(",", transpilers)).Observe("allPatchOwners", others)
            .Check(transpilers.Contains("kylemhb.storageframeworkwealthadjuster"), "the wealth transpiler is not applied to WealthWatcher.CalculateWealthItems");

        var crateDef = Kit.DefOrNull(CrateDef);
        ctx.Setup($"Integration condition: Reel's Expanded Storage active {ModsConfig.IsActive("reel.expanded.storage")}; {CrateDef} thingClass {crateDef?.thingClass?.FullName ?? "missing"}");
        if (crateDef == null || !typeof(ThingClass).IsAssignableFrom(crateDef.thingClass))
        {
            Kit.SkipRemaining(ctx, this, $"{CrateDef} is missing or not an ASF ThingClass", CoverageReasons.SetupFailed);
            return;
        }
        if (crateDef.researchPrerequisites != null) Kit.Research(ctx, crateDef.researchPrerequisites.Select(r => r.defName).ToArray());
        Kit.RemoveStartingColonists(ctx);
        var area = Kit.Area(ctx, 14, 6);
        IntVec3 At(int x, int z) => area.Min + new IntVec3(x, 0, z);
        var crate = Kit.Place(ctx, crateDef, At(1, 1), Rot4.North, ThingDefOf.WoodLog);
        IntVec3 floor = At(5, 1), floor2 = At(7, 1), zoneCell = At(10, 1);
        ctx.Setup($"Steel stacks of {Stack}; wealth read with WealthWatcher.ForceRecount after each spawn; stored percent set to 50 in memory (settings file not written)");

        // Floor and stored contributions.
        float unit = 0f;
        float full = Delta(map, () => { var s = SpawnSteel(map, Stack, floor); unit = s.MarketValue; return new[] { s }; });
        ctx.Begin("sfwa.floor").Expect("contribution", Stack * unit).Observe("contribution", full).Observe("unitMarketValue", unit)
            .Check(unit > 0f && Kit.Near(full, Stack * unit, Tolerance), "a floor stack does not contribute its ordinary value");

        float storedUnit = 0f;
        bool slotIsCrate = false;
        float stored = Delta(map, () =>
        {
            var s = SpawnSteel(map, Stack, crate.Position);
            storedUnit = s.MarketValue;
            slotIsCrate = s.StoringAdaptiveStorageOrNull() == crate;
            return new[] { s };
        });
        ctx.Begin("sfwa.stored").Expect("contribution", 0.5f * full).Expect("lookupFindsCrate", true)
            .Observe("contribution", stored).Observe("lookupFindsCrate", slotIsCrate)
            .Check(slotIsCrate && Kit.Near(stored, 0.5f * full, Tolerance), "a stack in ASF storage is not counted at 50%");

        float split = Delta(map, () => new[] { SpawnSteel(map, Stack, crate.Position), SpawnSteel(map, 25, floor2) });
        float splitExpected = 0.5f * Stack * unit + 25 * unit;
        ctx.Begin("sfwa.split").Expect("contribution", splitExpected).Observe("contribution", split)
            .Check(Kit.Near(split, splitExpected, Tolerance), "only the stored portion of a split stack should be discounted");

        var filter = ((IStoreSettingsParent)crate).GetStoreSettings().filter;
        bool wasAllowed = filter.Allows(ThingDefOf.Steel);
        filter.SetAllow(ThingDefOf.Steel, false);
        bool rejects = !((IStoreSettingsParent)crate).GetStoreSettings().AllowedToAccept(Steel(1));
        float rejected = Delta(map, () => new[] { SpawnSteel(map, Stack, crate.Position) });
        filter.SetAllow(ThingDefOf.Steel, wasAllowed);
        ctx.Setup($"Developer disallowed Steel in {crate.ThingID}'s storage filter for the rejected case, then restored it to {wasAllowed}");
        ctx.Begin("sfwa.rejected").Expect("filterRejects", true).Expect("contribution", 0.5f * full)
            .Observe("filterRejects", rejects).Observe("contribution", rejected)
            .Check(rejects && Kit.Near(rejected, 0.5f * full, Tolerance), "a filter-rejected stack physically in ASF storage should stay discounted");

        // A colonist standing on the crate cell carrying the stack.
        if (!crate.Position.Walkable(map))
            ctx.Begin("sfwa.carried").NotApplicable($"{CrateDef} cell is not walkable, so a pawn cannot stand on it");
        else
        {
            var pawn = Kit.Worker(ctx, crate.Position, WorkTypeDefOf.Hauling);
            pawn.workSettings.SetPriority(WorkTypeDefOf.Hauling, 0);
            bool carrying = false;
            float carried = Delta(map, () =>
            {
                var s = Steel(Stack);
                carrying = pawn.carryTracker.TryStartCarry(s) && pawn.carryTracker.CarriedThing == s && pawn.Position == crate.Position;
                return new[] { s };
            });
            pawn.Destroy(DestroyMode.Vanish);
            ctx.Setup("Developer destroyed the carrying colonist after the carried case");
            ctx.Begin("sfwa.carried").Expect("carryingOnStorageCell", true).Expect("contribution", full)
                .Observe("carryingOnStorageCell", carrying).Observe("contribution", carried)
                .Check(carrying && Kit.Near(carried, full, Tolerance), "a stack carried over ASF storage should count in full");
        }

        var zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        map.zoneManager.RegisterZone(zone);
        zone.AddCell(zoneCell);
        ctx.Setup($"Developer created a vanilla stockpile zone at {zoneCell}");
        float inZone = Delta(map, () => new[] { SpawnSteel(map, Stack, zoneCell) });
        zone.Delete();
        ctx.Begin("sfwa.stockpile").Expect("contribution", full).Observe("contribution", inZone)
            .Check(Kit.Near(inZone, full, Tolerance), "a stack in vanilla stockpile storage should count in full");

        // Settings percentages.
        var readings = new List<string>();
        bool settingsOk = true;
        foreach (int percent in new[] { 0, 25, 100 })
        {
            WealthAdjusterMod.Settings.storedPercent = percent;
            float d = Delta(map, () => new[] { SpawnSteel(map, Stack, crate.Position) });
            readings.Add($"{percent}%={CaseScope.Format(d)}");
            settingsOk &= Kit.Near(d, percent / 100f * full, Tolerance);
        }
        WealthAdjusterMod.Settings.storedPercent = 50;
        ctx.Begin("sfwa.settings").Expect("contributions", $"0%=0, 25%={CaseScope.Format(0.25f * full)}, 100%={CaseScope.Format(full)}")
            .Observe("contributions", string.Join(", ", readings))
            .Check(settingsOk, "stored contribution does not follow the configured percentage");

        ctx.Begin("sfwa.market-value").Expect("storedUnitMarketValue", unit).Observe("storedUnitMarketValue", storedUnit)
            .Check(unit > 0f && Kit.Near(storedUnit, unit, 0.0001f), "item market value changed while stored");

        map.wealthWatcher.ForceRecount(false);
        float emptyValue = crate.MarketValue, emptyBuildings = map.wealthWatcher.WealthBuildings;
        var fill = SpawnSteel(map, Stack, crate.Position);
        map.wealthWatcher.ForceRecount(false);
        float fullValue = crate.MarketValue, fullBuildings = map.wealthWatcher.WealthBuildings;
        fill.Destroy();
        ctx.Begin("sfwa.building-wealth").Expect("crateMarketValueChange", 0f).Expect("buildingWealthChange", 0f)
            .Observe("crateMarketValueChange", fullValue - emptyValue).Observe("buildingWealthChange", fullBuildings - emptyBuildings)
            .Check(Kit.Near(fullValue, emptyValue, 0.0001f) && Kit.Near(fullBuildings, emptyBuildings, Tolerance),
                "the storage building's own wealth changed with its contents");

        var kept = SpawnSteel(map, Stack, crate.Position);
        ctx.Setup($"Developer left {Stack} Steel ({kept.ThingID}) in {crate.ThingID} before save/reload");
        ctx.Remember("crate", crate.ThingID);
        ctx.Remember("stack", kept.ThingID);
        ctx.Remember("unit", CaseScope.Format(unit));
    }

    private void Reloaded(BatchContext ctx)
    {
        var map = ctx.Map;
        if (ctx.ResultOf("sfwa.reload") != null) return;
        var crate = map.listerThings.AllThings.FirstOrDefault(t => t.ThingID == ctx.Recall("crate"));
        var stack = map.listerThings.AllThings.FirstOrDefault(t => t.ThingID == ctx.Recall("stack"));
        if (!Kit.Require(ctx, "sfwa.reload", (crate != null, "crate survived save/reload"), (stack != null && stack.Position == crate?.Position, "stack still in the crate after save/reload")))
            return;
        float unit = ctx.RecallFloat("unit");
        float with = Items(map);
        int count = stack.stackCount;
        stack.Destroy();
        float without = Items(map);
        float expected = 0.5f * count * unit;
        ctx.Begin("sfwa.reload").Expect("contribution", expected).Expect("count", Stack)
            .Observe("contribution", with - without).Observe("count", count)
            .Check(count == Stack && Kit.Near(with - without, expected, Tolerance), "stored discount differs after save/reload");
    }
}

internal static class AsfLookup
{
    public static Thing StoringAdaptiveStorageOrNull(this Thing thing) => AdaptiveStorage.Utility.ThingExtensions.StoringAdaptiveStorage(thing);
}
