using Analyzer.Domain;
using Microsoft.Extensions.Logging;

namespace Analyzer.Rules;

/// <summary>
/// Detects sustained high CPU usage using a sliding window of process_cpu_seconds_total rate.
/// </summary>
public class CpuSpikeRule
{
    private readonly Queue<MetricSample> _window = new();
    private readonly ILogger<CpuSpikeRule> _logger;
    private const int WindowSize = 10;                // ~50s of data at 5s intervals
    private const double ThresholdCpuPercent = 80.0;  // 80% sustained CPU triggers alert
    private const int MinSamplesAbove = 7;            // 7 out of 10 must be above threshold
    private DateTime _lastAlertTime = DateTime.MinValue;
    private static readonly TimeSpan CooldownPeriod = TimeSpan.FromMinutes(5);

    public CpuSpikeRule(ILogger<CpuSpikeRule> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Evaluate CPU usage percentage (0-100+).
    /// </summary>
    public FailureEvent? Evaluate(double cpuPercent)
    {
        _window.Enqueue(new MetricSample
        {
            Value = cpuPercent,
            Timestamp = DateTime.UtcNow
        });

        if (_window.Count > WindowSize)
            _window.Dequeue();

        if (_window.Count < WindowSize)
        {
            _logger.LogDebug("CPU window filling: {Count}/{Size}, value={Value:F2}%",
                _window.Count, WindowSize, cpuPercent);
            return null;
        }

        if (DateTime.UtcNow - _lastAlertTime < CooldownPeriod)
        {
            _logger.LogDebug("CPU rule in cooldown period, skipping");
            return null;
        }

        var samplesAbove = _window.Count(s => s.Value > ThresholdCpuPercent);
        var avgCpu = _window.Average(s => s.Value);
        var maxCpu = _window.Max(s => s.Value);

        _logger.LogInformation(
            "CPU analysis: avg={Avg:F2}%, max={Max:F2}%, above_threshold={Above}/{Total} (threshold={Threshold}%)",
            avgCpu, maxCpu, samplesAbove, WindowSize, ThresholdCpuPercent);

        if (samplesAbove >= MinSamplesAbove)
        {
            _lastAlertTime = DateTime.UtcNow;

            var severity = maxCpu > 95 ? "Critical" : "Warning";

            return new FailureEvent
            {
                FailureType = "HighCpuUsage",
                Severity = severity,
                Description = $"CPU sustained at {avgCpu:F1}% (peak {maxCpu:F1}%) for {samplesAbove}/{WindowSize} samples"
            };
        }

        return null;
    }
}
