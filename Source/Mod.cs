using HarmonyLib;
using UnityEngine;
using Verse;

namespace StorageFrameworkWealthAdjuster;

public class WealthAdjusterSettings : ModSettings
{
    public const int DefaultPercent = 50;
    public int storedPercent = DefaultPercent;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref storedPercent, "storedPercent", DefaultPercent);
        storedPercent = Mathf.Clamp(storedPercent, 0, 100);
    }
}

public class WealthAdjusterMod : Mod
{
    public static WealthAdjusterSettings Settings;

    public WealthAdjusterMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<WealthAdjusterSettings>();
        new Harmony("kylemhb.storageframeworkwealthadjuster").PatchAll(typeof(WealthAdjusterMod).Assembly);
    }

    public override string SettingsCategory() => "Adaptive Storage Framework Wealth Adjuster";

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var list = new Listing_Standard();
        list.Begin(inRect);
        float value = list.SliderLabeled("SFWA_StoredPercentLabel".Translate(Settings.storedPercent),
            Settings.storedPercent, 0f, 100f, 0.5f, "SFWA_StoredPercentHelp".Translate());
        Settings.storedPercent = Mathf.Clamp(Mathf.RoundToInt(value), 0, 100);
        list.End();
    }
}
