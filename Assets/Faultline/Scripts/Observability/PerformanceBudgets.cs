namespace Faultline.Observability
{
    /// <summary>
    /// Performance budgets per target device profile (PRD §13).
    ///
    /// NOTE: Editor performance does NOT reflect device performance. Budgets
    /// below are device targets and must be validated on-device, not in the
    /// Unity Editor. These are documentation + stub values; the actual probe
    /// wiring is added when the platform integration lands.
    /// </summary>
    public static class PerformanceBudgets
    {
        // 90 Hz refresh => 11.1 ms frame budget on Meta Quest.
        public const double QuestFrameTimeMs = 11.1;
        public const int QuestDrawCalls = 150;
        public const int QuestMaterialCount = 75;
        public const long QuestMemoryBytes = 1024L * 1024 * 1024; // 1 GB app budget

        // Meta VR Glasses: tighter thermal/envelope constraints.
        public const double MetaVrGlassesFrameTimeMs = 16.7; // 60 Hz target
        public const int MetaVrGlassesDrawCalls = 80;
        public const int MetaVrGlassesMaterialCount = 40;
        public const long MetaVrGlassesMemoryBytes = 512L * 1024 * 1024; // 512 MB app budget

        /// <summary>Budget profile for Meta Quest (90 Hz).</summary>
        public static PerformanceBudget ForQuest() => new PerformanceBudget(
            "Quest",
            frameTimeMs: QuestFrameTimeMs,
            cpuMs: 9.0,
            gpuMs: 9.0,
            memoryBytes: QuestMemoryBytes,
            drawCalls: QuestDrawCalls,
            materialCount: QuestMaterialCount,
            particleCount: 5000,
            interactionLatencyMs: 20.0,
            sceneInitTimeMs: 250.0);

        /// <summary>Budget profile for Meta VR Glasses (60 Hz).</summary>
        public static PerformanceBudget ForMetaVrGlasses() => new PerformanceBudget(
            "Meta VR Glasses",
            frameTimeMs: MetaVrGlassesFrameTimeMs,
            cpuMs: 13.0,
            gpuMs: 13.0,
            memoryBytes: MetaVrGlassesMemoryBytes,
            drawCalls: MetaVrGlassesDrawCalls,
            materialCount: MetaVrGlassesMaterialCount,
            particleCount: 2000,
            interactionLatencyMs: 30.0,
            sceneInitTimeMs: 400.0);

        /// <summary>
        /// All known device profiles. Used by test/discovery tooling to
        /// enumerate supported targets.
        /// </summary>
        public static IReadOnlyList<PerformanceBudget> AllProfiles()
        {
            return new List<PerformanceBudget> { ForQuest(), ForMetaVrGlasses() }
                .AsReadOnly();
        }
    }
}
