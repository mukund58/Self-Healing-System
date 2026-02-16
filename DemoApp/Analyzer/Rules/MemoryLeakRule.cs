using Analyzer.Domain;

namespace Analyzer.Rules;

public class MemoryLeakRule
{
    private readonly Queue<MetricSample> _window = new();
    private const int WindowSize = 5;
    private const double ThresholdMbPerMinute = 20;

    public FailureEvent? Evaluate(double currentValue)
    {
        _window.Enqueue(new MetricSample
        {
            Value = currentValue,
            Timestamp = DateTime.UtcNow
        });

        if (_window.Count > WindowSize)
            _window.Dequeue();

        if (_window.Count < WindowSize)
            return null;

        var first = _window.First();
        var last = _window.Last();

        // Values from Prometheus are already in MB
        var deltaMb = last.Value - first.Value;
        var minutes = (last.Timestamp - first.Timestamp).TotalMinutes;

        if (minutes <= 0)
            return null;

        var slope = deltaMb / minutes;

        if (slope > ThresholdMbPerMinute)
        {
            return new FailureEvent
            {
                FailureType = "MemoryLeakSuspected",
                Severity = "Warning",
                Description = $"Memory growing at {slope:F2} MB/min"
            };
        }

        return null;
    }
}
