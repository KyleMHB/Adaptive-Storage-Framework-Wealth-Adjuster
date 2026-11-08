using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AdaptiveStorage;
using AdaptiveStorage.Utility;
using HarmonyLib;
using RimWorld;
using Verse;

namespace StorageFrameworkWealthAdjuster;

// Swaps the one Thing.MarketValue call in the item-wealth sum for StoredWealth.UnitValue.
// Vanilla's enumeration, filters and stackCount multiplication stay untouched.
[HarmonyPatch(typeof(WealthWatcher), "CalculateWealthItems")]
public static class CalculateWealthItemsPatch
{
    static readonly MethodInfo MarketValueGetter = AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.MarketValue));
    static readonly MethodInfo Helper = AccessTools.Method(typeof(StoredWealth), nameof(StoredWealth.UnitValue));

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var list = instructions.ToList();
        var matches = list.Where(i => i.Calls(MarketValueGetter)).ToList();
        if (matches.Count != 1)
        {
            Log.Error($"[Adaptive Storage Framework Wealth Adjuster] Expected one Thing.MarketValue call in WealthWatcher.CalculateWealthItems but found {matches.Count}; stored item wealth will not be adjusted.");
            return list;
        }
        // Mutate in place so labels and exception blocks stay attached.
        matches[0].opcode = OpCodes.Call;
        matches[0].operand = Helper;
        return list;
    }
}

public static class StoredWealth
{
    public static float UnitValue(Thing thing)
    {
        float value = thing.MarketValue;
        int percent = WealthAdjusterMod.Settings?.storedPercent ?? WealthAdjusterSettings.DefaultPercent;
        if (percent >= 100 || !thing.Spawned)
            return value;
        ThingClass storage = thing.StoringAdaptiveStorage();
        if (storage == null || !storage.Spawned || storage.Map != thing.Map)
            return value;
        return value * (percent / 100f);
    }
}
