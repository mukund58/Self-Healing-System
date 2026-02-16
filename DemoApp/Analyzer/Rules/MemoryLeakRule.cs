using Analyzer.Domain;
using Microsoft.Extensions.Logging;

namespace Analyzer.Rules;

public class MemoryLeakRule
{
    private readonly Queue<MetricSample> _window = new();
    private readonly ILogger<MemoryLeakRule> _logger;
    private const int WindowSize = 8;           // ~40s of data at 5s intervals
    private const double ThresholdMbPerMinute = 15; // tuned: triggers on real stress, ignores normal startup
    // private const double ThresholdMbPerMinute = 50; // Keep 50 for production
    private DateTime _lastAlertTime = DateTime.MinValue;
    private static readonly TimeSpan CooldownPeriod = TimeSpan.FromMinutes(3); // 3-min cooldown to prevent restart loops

    public MemoryLeakRule(ILogger<MemoryLeakRule> logger)
    {
        _logger = logger;
    }

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
        {
            _logger.LogDebug("Window filling: {Count}/{Size}, value={Value:F2}MB", 
                _window.Count, WindowSize, currentValue);
            return null;
        }

        // Cooldown: don't alert if we alerted recently
        if (DateTime.UtcNow - _lastAlertTime < CooldownPeriod)
        {
            _logger.LogDebug("In cooldown period, skipping evaluation");
            return null;
        }

        var first = _window.First();
        var last = _window.Last();

        // Values from Prometheus are already in MB
        var deltaMb = last.Value - first.Value;
        var minutes = (last.Timestamp - first.Timestamp).TotalMinutes;

        if (minutes <= 0)
            return null;

        var slope = deltaMb / minutes;

        _logger.LogInformation("Memory analysis: first={First:F2}MB, last={Last:F2}MB, delta={Delta:F2}MB, minutes={Min:F2}, slope={Slope:F2} MB/min (threshold={Threshold})",
            first.Value, last.Value, deltaMb, minutes, slope, ThresholdMbPerMinute);

        if (slope > ThresholdMbPerMinute)
        {
            _lastAlertTime = DateTime.UtcNow;
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
