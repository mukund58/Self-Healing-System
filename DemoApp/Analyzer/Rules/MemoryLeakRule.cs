using Analyzer.Domain;
using Microsoft.Extensions.Logging;

namespace Analyzer.Rules;

/// <summary>
/// Detects memory leaks using ordinary least-squares (OLS) linear regression
/// over a sliding window of metric samples. Alerts only when the regression
/// slope exceeds a threshold AND the R² confidence is high enough — meaning
/// the upward trend is statistically consistent, not just a noisy spike.
/// </summary>
public class MemoryLeakRule
{
    private readonly Queue<MetricSample> _window = new();
    private readonly ILogger<MemoryLeakRule> _logger;

    // ── Tuning knobs ────────────────────────────────────────────────
    private const int WindowSize = 20;                // ~100s at 5s intervals — enough for meaningful regression
    private const int MinSamplesForRegression = 10;   // need at least this many before we trust the fit
    private const double SlopeMbPerMinute = 15;       // trigger: sustained growth rate
    private const double MinR2 = 0.60;                // confidence: at least 60% of variance explained by trend
    private const double CriticalSlopeMbPerMinute = 50; // escalate to Critical severity
    private const double HighConfidenceR2 = 0.85;     // high-confidence qualifier for severity upgrade

    private DateTime _lastAlertTime = DateTime.MinValue;
    private static readonly TimeSpan CooldownPeriod = TimeSpan.FromMinutes(3);

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

        if (_window.Count < MinSamplesForRegression)
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

        // ── OLS Linear Regression ───────────────────────────────────
        // x = elapsed minutes from first sample, y = memory MB
        var samples = _window.ToArray();
        var t0 = samples[0].Timestamp;
        int n = samples.Length;

        double sumX = 0, sumY = 0, sumXX = 0, sumXY = 0, sumYY = 0;
        for (int i = 0; i < n; i++)
        {
            double x = (samples[i].Timestamp - t0).TotalMinutes;
            double y = samples[i].Value;
            sumX += x;
            sumY += y;
            sumXX += x * x;
            sumXY += x * y;
            sumYY += y * y;
        }

        double denominator = n * sumXX - sumX * sumX;
        if (Math.Abs(denominator) < 1e-12)
            return null; // all samples at same timestamp — degenerate

        // slope (MB per minute) and intercept
        double slope = (n * sumXY - sumX * sumY) / denominator;
        double intercept = (sumY - slope * sumX) / n;

        // R² — coefficient of determination
        // R² = 1 − SS_res / SS_tot
        double meanY = sumY / n;
        double ssTot = sumYY - n * meanY * meanY;       // total sum of squares
        double ssRes = 0;
        for (int i = 0; i < n; i++)
        {
            double x = (samples[i].Timestamp - t0).TotalMinutes;
            double predicted = intercept + slope * x;
            double residual = samples[i].Value - predicted;
            ssRes += residual * residual;
        }
        double r2 = ssTot > 1e-12 ? 1.0 - ssRes / ssTot : 0;

        double windowMinutes = (samples[^1].Timestamp - t0).TotalMinutes;

        _logger.LogInformation(
            "Memory regression: n={N}, window={Window:F1}min, slope={Slope:F2} MB/min, R²={R2:F3}, " +
            "range=[{Min:F1}–{Max:F1}]MB (thresholds: slope>{SlopeThresh}, R²>{R2Thresh})",
            n, windowMinutes, slope, r2,
            samples.Min(s => s.Value), samples.Max(s => s.Value),
            SlopeMbPerMinute, MinR2);

        // ── Decision: slope AND confidence must both exceed thresholds ──
        if (slope > SlopeMbPerMinute && r2 > MinR2)
        {
            _lastAlertTime = DateTime.UtcNow;

            // Severity: Critical if slope is extreme AND confidence is very high
            bool isCritical = slope > CriticalSlopeMbPerMinute && r2 > HighConfidenceR2;
            string severity = isCritical ? "Critical" : "Warning";

            return new FailureEvent
            {
                FailureType = "MemoryLeakSuspected",
                Severity = severity,
                Description = $"Memory trending +{slope:F1} MB/min (R²={r2:F2}, n={n}, {windowMinutes:F1}min window)"
            };
        }

        return null;
    }
}
