using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveStorage;
using RimWorld;
using RimWorldDevTests;
using Verse;

namespace StorageFrameworkWealthAdjuster.DevTests;

// Survey of every loaded storage building (Building_Storage and subclasses). For each def the developer spawns the building
// directly (no designator, research or construction), spawns 75 Steel on its first storage cell and forces a recount.
// Expected (docs/spec.md): ASF ThingClass storage counts 50% at the default setting; every other storage counts 100%.
// Mechanical only: placement rules, power, fixed filters and pawn hauling are not exercised.
public class StorageSurveyBatch : Batch
{
    private const int Stack = 75, AreaSize = 16;
    private const float RatioTolerance = 0.001f;

    public override string Id => "sfwa-storage-survey";
    public override int SegmentCount => 1;
    public override int MapSize => 100;
    public override TimeSpan WallLimit => TimeSpan.FromMinutes(30);
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[]
    {
        new PlannedCase("sfwa.survey-asf", Levels.Mechanical),
        new PlannedCase("sfwa.survey-other", Levels.Mechanical),
    };

    private record Reading(ThingDef Def, bool Asf, string Outcome, float Ratio);

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        var settings = WealthAdjusterMod.Settings;
        int original = settings.storedPercent;
        try
        {
            settings.storedPercent = 50;
            Run(ctx);
        }
        finally
        {
            settings.storedPercent = original;
        }
        yield break;
    }

    private static float Items(Map map)
    {
        map.wealthWatcher.ForceRecount(false);
        return map.wealthWatcher.WealthItems;
    }

    private static Thing Steel()
    {
        var t = ThingMaker.MakeThing(ThingDefOf.Steel);
        t.stackCount = Stack;
        return t;
    }

    private void Run(BatchContext ctx)
    {
        var map = ctx.Map;
        var defs = DefDatabase<ThingDef>.AllDefsListForReading
            .Where(d => d.category == ThingCategory.Building && d.thingClass != null && typeof(Building_Storage).IsAssignableFrom(d.thingClass))
            .OrderBy(d => d.defName).ToList();
        ctx.Setup($"{defs.Count} loaded storage building defs ({defs.Count(IsAsf)} ASF); {ModsConfig.ActiveModsInLoadOrder.Count()} active mods");
        Kit.RemoveStartingColonists(ctx);
        var area = Kit.Area(ctx, AreaSize, AreaSize);
        var center = area.CenterCell;
        var floorCell = area.Min;

        float before = Items(map);
        var probe = GenSpawn.Spawn(Steel(), floorCell, map);
        float full = Items(map) - before;
        probe.Destroy();
        ctx.Setup($"Floor reference: {Stack} Steel contributes {CaseScope.Format(full)} item wealth");

        var readings = new List<Reading>();
        foreach (var def in defs)
        {
            readings.Add(Measure(ctx, def, center, area, full));
            foreach (var t in area.Cells.SelectMany(c => c.GetThingList(map)).Where(t => t.def.category is ThingCategory.Item or ThingCategory.Building or ThingCategory.Filth).Distinct().ToList())
                if (!t.Destroyed) t.Destroy(DestroyMode.Vanish);
        }

        foreach (var r in readings)
            ctx.Setup($"{(r.Asf ? "ASF" : "other")} {r.Def.defName} [{r.Def.modContentPack?.Name ?? "?"}] {r.Def.thingClass.Name}: {r.Outcome}{(r.Outcome == "measured" ? " ratio " + CaseScope.Format(r.Ratio) : "")}");

        Report(ctx, "sfwa.survey-asf", readings.Where(r => r.Asf).ToList(), 0.5f);
        Report(ctx, "sfwa.survey-other", readings.Where(r => !r.Asf).ToList(), 1f);
    }

    private static bool IsAsf(ThingDef def) => typeof(ThingClass).IsAssignableFrom(def.thingClass);

    private static Reading Measure(BatchContext ctx, ThingDef def, IntVec3 center, CellRect area, float full)
    {
        var map = ctx.Map;
        bool asf = IsAsf(def);
        if (def.Size.x > AreaSize - 2 || def.Size.z > AreaSize - 2) return new Reading(def, asf, "skipped: larger than test area", 0f);
        Thing building;
        try
        {
            var stuff = def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null;
            building = ThingMaker.MakeThing(def, stuff);
            if (def.CanHaveFaction) building.SetFaction(Faction.OfPlayer);
            building = GenSpawn.Spawn(building, center, map, Rot4.North, WipeMode.Vanish);
        }
        catch (Exception e)
        {
            return new Reading(def, asf, "spawn failed: " + e.GetType().Name + " " + e.Message.Split('\n')[0], 0f);
        }
        try
        {
            if (building is not ISlotGroupParent parent) return new Reading(def, asf, "no slot group", 0f);
            var cell = parent.AllSlotCellsList()?.Cast<IntVec3?>().FirstOrDefault(c => area.Contains(c.Value));
            if (cell == null) return new Reading(def, asf, "no storage cell", 0f);
            float before = Items(map);
            var steel = GenSpawn.Spawn(Steel(), cell.Value, map);
            float after = Items(map);
            bool onCell = steel.Spawned && steel.Position == cell.Value;
            if (!steel.Destroyed) steel.Destroy();
            return new Reading(def, asf, onCell ? "measured" : "steel not on storage cell after spawn", (after - before) / full);
        }
        catch (Exception e)
        {
            return new Reading(def, asf, "measure failed: " + e.GetType().Name + " " + e.Message.Split('\n')[0], 0f);
        }
    }

    private static void Report(BatchContext ctx, string id, List<Reading> readings, float expected)
    {
        var measured = readings.Where(r => r.Outcome == "measured").ToList();
        var wrong = measured.Where(r => Math.Abs(r.Ratio - expected) > RatioTolerance).ToList();
        var unmeasured = readings.Where(r => r.Outcome != "measured").ToList();
        var scope = ctx.Begin(id).Expect("ratio", expected).Expect("wrongRatio", 0)
            .Observe("defs", readings.Count).Observe("measured", measured.Count).Observe("unmeasured", unmeasured.Count)
            .Observe("wrongRatio", wrong.Count)
            .Observe("wrongDefs", wrong.Count == 0 ? "none" : string.Join(", ", wrong.Select(r => $"{r.Def.defName}={CaseScope.Format(r.Ratio)}")))
            .Observe("unmeasuredDefs", unmeasured.Count == 0 ? "none" : string.Join("; ", unmeasured.Select(r => $"{r.Def.defName}: {r.Outcome}")));
        if (readings.Count == 0) { scope.NotApplicable("no storage defs of this kind are loaded"); return; }
        scope.Check(measured.Count > 0 && wrong.Count == 0, $"{wrong.Count} of {measured.Count} measured defs differ from ratio {expected}");
    }
}
