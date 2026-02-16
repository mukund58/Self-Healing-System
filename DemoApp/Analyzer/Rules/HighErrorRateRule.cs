using Analyzer.Domain;
using Microsoft.Extensions.Logging;

namespace Analyzer.Rules;

/// <summary>
/// Detects high HTTP error rates (5xx responses) using Prometheus http_requests_received_total.
/// </summary>
public class HighErrorRateRule
{
    private readonly Queue<MetricSample> _errorRateWindow = new();
    private readonly ILogger<HighErrorRateRule> _logger;
    private const int WindowSize = 6;                 // ~30s at 5s intervals
    private const double ThresholdErrorPercent = 10.0; // 10% error rate triggers alert
    private const int MinSamplesAbove = 4;             // 4 out of 6 must be above threshold
    private DateTime _lastAlertTime = DateTime.MinValue;
    private static readonly TimeSpan CooldownPeriod = TimeSpan.FromMinutes(3);

    public HighErrorRateRule(ILogger<HighErrorRateRule> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Evaluate error rate as a percentage (0-100).
    /// </summary>
    public FailureEvent? Evaluate(double errorRatePercent)
    {
        _errorRateWindow.Enqueue(new MetricSample
        {
            Value = errorRatePercent,
            Timestamp = DateTime.UtcNow
        });

        if (_errorRateWindow.Count > WindowSize)
            _errorRateWindow.Dequeue();

        if (_errorRateWindow.Count < WindowSize)
        {
            _logger.LogDebug("Error rate window filling: {Count}/{Size}, value={Value:F2}%",
                _errorRateWindow.Count, WindowSize, errorRatePercent);
            return null;
        }

        if (DateTime.UtcNow - _lastAlertTime < CooldownPeriod)
        {
            _logger.LogDebug("Error rate rule in cooldown period, skipping");
            return null;
        }

        var samplesAbove = _errorRateWindow.Count(s => s.Value > ThresholdErrorPercent);
        var avgRate = _errorRateWindow.Average(s => s.Value);
        var maxRate = _errorRateWindow.Max(s => s.Value);

        _logger.LogInformation(
            "Error rate analysis: avg={Avg:F2}%, max={Max:F2}%, above_threshold={Above}/{Total}",
            avgRate, maxRate, samplesAbove, WindowSize);

        if (samplesAbove >= MinSamplesAbove)
        {
            _lastAlertTime = DateTime.UtcNow;

            var severity = maxRate > 50 ? "Critical" : "Warning";

            return new FailureEvent
            {
                FailureType = "HighErrorRate",
                Severity = severity,
                Description = $"Error rate at {avgRate:F1}% (peak {maxRate:F1}%) for {samplesAbove}/{WindowSize} samples"
            };
        }

        return null;
    }
}
