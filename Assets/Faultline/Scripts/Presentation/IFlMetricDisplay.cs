namespace Faultline.Presentation
{
    using System;

    /// <summary>
    /// Numeric metric display contract (latency, error rate, CPU, memory, ...).
    ///
    /// Visual states: Normal / Hover / Selected / Disabled / Critical.
    /// Shows a formatted value with a technical-label glyph (JetBrains Mono).
    /// Enters Critical when the metric breaches its SLO threshold.
    ///
    /// Pure C# — Unity TextMeshPro implemented behind this interface.
    /// </summary>
    public interface IFlMetricDisplay
    {
        VisualState CurrentState { get; }

        /// <summary>Metric key / id.</summary>
        string MetricId { get; }

        /// <summary>Formatted display value, e.g. "240 ms".</summary>
        string DisplayValue { get; set; }

        /// <summary>Unit label, e.g. "ms", "%".</summary>
        string Unit { get; set; }

        /// <summary>SLO threshold; breaching it moves state to Critical.</summary>
        double? CriticalThreshold { get; set; }

        event Action<IFlMetricDisplay, VisualState> StateChanged;
        event Action<IFlMetricDisplay> ValueChanged;

        void SetState(VisualState state);

        /// <summary>Update the displayed value; re-evaluate Critical state against threshold.</summary>
        void SetValue(double value, string formatted);

        void OnPointerEnter();
        void OnPointerExit();

        bool ValidateConfiguration(out string errorMessage);
    }
}
