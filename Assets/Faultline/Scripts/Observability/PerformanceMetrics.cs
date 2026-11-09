namespace Faultline.Observability
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// A single budget violation recorded when a measured metric exceeds its
    /// configured budget for the active device profile.
    /// </summary>
    public sealed class BudgetViolation
    {
        public string MetricName { get; }
        public double Measured { get; }
        public double? Budget { get; }
        public string DeviceProfile { get; }
        public DateTimeOffset Timestamp { get; }

        public BudgetViolation(string metricName, double measured, double? budget,
            string deviceProfile, DateTimeOffset timestamp)
        {
            MetricName = metricName;
            Measured = measured;
            Budget = budget;
            DeviceProfile = deviceProfile;
            Timestamp = timestamp;
        }
    }

    /// <summary>
    /// Performance budget for a target device profile. Null fields mean
    /// "no budget enforced" for that metric.
    /// </summary>
    public sealed class PerformanceBudget
    {
        public string DeviceProfile { get; }
        public double? FrameTimeMs { get; }
        public double? CpuMs { get; }
        public double? GpuMs { get; }
        public long? MemoryBytes { get; }
        public int? DrawCalls { get; }
        public int? MaterialCount { get; }
        public int? ParticleCount { get; }
        public double? InteractionLatencyMs { get; }
        public double? SceneInitTimeMs { get; }

        public PerformanceBudget(string deviceProfile,
            double? frameTimeMs = null, double? cpuMs = null, double? gpuMs = null,
            long? memoryBytes = null, int? drawCalls = null, int? materialCount = null,
            int? particleCount = null, double? interactionLatencyMs = null,
            double? sceneInitTimeMs = null)
        {
            DeviceProfile = deviceProfile;
            FrameTimeMs = frameTimeMs;
            CpuMs = cpuMs;
            GpuMs = gpuMs;
            MemoryBytes = memoryBytes;
            DrawCalls = drawCalls;
            MaterialCount = materialCount;
            ParticleCount = particleCount;
            InteractionLatencyMs = interactionLatencyMs;
            SceneInitTimeMs = sceneInitTimeMs;
        }

        public double? GetBudget(string metricName) => metricName switch
        {
            "frame_time_ms" => FrameTimeMs,
            "cpu_ms" => CpuMs,
            "gpu_ms" => GpuMs,
            "memory_bytes" => MemoryBytes.HasValue ? (double)MemoryBytes.Value : (double?)null,
            "draw_calls" => DrawCalls.HasValue ? (double)DrawCalls.Value : (double?)null,
            "material_count" => MaterialCount.HasValue ? (double)MaterialCount.Value : (double?)null,
            "particle_count" => ParticleCount.HasValue ? (double)ParticleCount.Value : (double?)null,
            "interaction_latency_ms" => InteractionLatencyMs,
            "scene_init_time_ms" => SceneInitTimeMs,
            _ => null
        };
    }

    /// <summary>
    /// Records performance samples (frame time, CPU, GPU, memory, GC, draw
    /// calls, material count, particle count, interaction latency, scene init
    /// time) and tracks them against the active device-profile budget.
    ///
    /// All values are plain numbers; no engine types are referenced here. The
    /// real probes are filled in once the platform integration is wired up.
    /// </summary>
    public sealed class PerformanceMetrics
    {
        private readonly List<MetricSample> _samples = new();
        private readonly List<BudgetViolation> _violations = new();
        private PerformanceBudget _budget;

        public event Action<BudgetViolation> BudgetExceeded;

        public PerformanceBudget Budget => _budget;
        public IReadOnlyList<MetricSample> Samples => _samples.AsReadOnly();
        public IReadOnlyList<BudgetViolation> Violations => _violations.AsReadOnly();

        public void SetBudget(PerformanceBudget budget) => _budget = budget;

        public void RecordFrameTime(double ms) => Record("frame_time_ms", ms, "ms");
        public void RecordCpuMs(double ms) => Record("cpu_ms", ms, "ms");
        public void RecordGpuMs(double ms) => Record("gpu_ms", ms, "ms");
        public void RecordMemoryBytes(long bytes) => Record("memory_bytes", (double)bytes, "bytes");
        public void RecordGcBytes(long bytes) => Record("gc_bytes", (double)bytes, "bytes");
        public void RecordDrawCalls(int count) => Record("draw_calls", (double)count, "count");
        public void RecordMaterialCount(int count) => Record("material_count", (double)count, "count");
        public void RecordParticleCount(int count) => Record("particle_count", (double)count, "count");
        public void RecordInteractionLatencyMs(double ms) => Record("interaction_latency_ms", ms, "ms");
        public void RecordSceneInitTimeMs(double ms) => Record("scene_init_time_ms", ms, "ms");

        /// <summary>Record a generic named sample.</summary>
        public void Record(string name, double value, string unit = null)
        {
            _samples.Add(new MetricSample(name, value, unit ?? string.Empty));

            if (_budget != null)
            {
                var budgetValue = _budget.GetBudget(name);
                if (budgetValue.HasValue && value > budgetValue.Value)
                {
                    var violation = new BudgetViolation(name, value, budgetValue.Value,
                        _budget.DeviceProfile, DateTimeOffset.UtcNow);
                    _violations.Add(violation);
                    BudgetExceeded?.Invoke(violation);
                }
            }
        }

        public bool IsWithinBudget(string name, double value)
        {
            if (_budget == null)
            {
                return true;
            }
            var budgetValue = _budget.GetBudget(name);
            return !budgetValue.HasValue || value <= budgetValue.Value;
        }

        public double? GetLatest(string name)
        {
            return _samples.LastOrDefault(s => s.Name == name)?.Value;
        }

        public double GetAverage(string name)
        {
            var values = _samples.Where(s => s.Name == name).Select(s => s.Value);
            var list = values.ToList();
            return list.Count == 0 ? 0.0 : list.Average();
        }

        public void Clear()
        {
            _samples.Clear();
            _violations.Clear();
        }
    }
}
