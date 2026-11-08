using System;
using System.Collections.Generic;
using RimWorldDevTests;

namespace StorageFrameworkWealthAdjuster.DevTests;

public sealed class ModHarness : HarnessSettings
{
    public override string DisplayName => "Adaptive Storage Framework Wealth Adjuster";
    public override string ArgPrefix => "sfwa";
    public override string PackageId => "kylemhb.storageframeworkwealthadjuster";
    public override Type ProductionType => typeof(WealthAdjusterMod);
    public override string EvidenceFolder => "StorageFrameworkWealthAdjusterTests";
    public override string[] RuntimeFolders => new[] { "About", "Assemblies", "Languages" };
    public override string[] RuntimeFiles => Array.Empty<string>();
    public override IEnumerable<Batch> CreateBatches() => new Batch[] { new WealthBatch(), new StorageSurveyBatch() };
    public override IEnumerable<string> RunAll => new[] { "sfwa-wealth", "sfwa-storage-survey" };
}
