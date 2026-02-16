using System.Collections.Concurrent;

namespace Analyzer.Service;

/// <summary>
/// Learning loop that records failure→recovery outcomes and uses historical
/// success rates to recommend the best remediation strategy for each root cause.
/// Maintains an in-memory knowledge base of past interventions.
/// </summary>
public class LearningService
{
    private readonly ILogger<LearningService> _logger;

    // Key: rootCause, Value: dict of strategyName -> (successes, failures)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, StrategyRecord>> _history = new();

    // Recent events for API introspection
    private readonly ConcurrentQueue<LearningEvent> _events = new();
    private const int MaxEventHistory = 200;

    public LearningService(ILogger<LearningService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Record the outcome of a remediation attempt so future decisions can be informed.
    /// </summary>
    public void RecordOutcome(string rootCause, string strategyName, bool success)
    {
        var strategies = _history.GetOrAdd(rootCause, _ => new ConcurrentDictionary<string, StrategyRecord>());
        var record = strategies.GetOrAdd(strategyName, _ => new StrategyRecord { StrategyName = strategyName });

        if (success)
            Interlocked.Increment(ref record.Successes);
        else
            Interlocked.Increment(ref record.Failures);

        var evt = new LearningEvent
        {
            Timestamp = DateTime.UtcNow,
            RootCause = rootCause,
            StrategyName = strategyName,
            Success = success,
            NewSuccessRate = record.SuccessRate,
            TotalAttempts = record.TotalAttempts
        };

        _events.Enqueue(evt);
        while (_events.Count > MaxEventHistory)
            _events.TryDequeue(out _);

        _logger.LogInformation(
            "📚 Learning: {RootCause} + {Strategy} → {Outcome} " +
            "(success rate: {Rate:P0}, total: {Total})",
            rootCause, strategyName, success ? "✅ SUCCESS" : "❌ FAIL",
            record.SuccessRate, record.TotalAttempts);
    }

    /// <summary>
    /// Get the best-performing strategy for a given root cause.
    /// Returns null if there's no history yet.
    /// </summary>
    public StrategyRecommendation? GetBestStrategy(string rootCause)
    {
        if (!_history.TryGetValue(rootCause, out var strategies) || strategies.IsEmpty)
            return null;

        // Find the strategy with the highest success rate (with at least 1 attempt)
        var best = strategies.Values
            .Where(s => s.TotalAttempts > 0)
            .OrderByDescending(s => s.SuccessRate)
            .ThenByDescending(s => s.TotalAttempts) // prefer more data
            .FirstOrDefault();

        if (best is null) return null;

        return new StrategyRecommendation
        {
            StrategyName = best.StrategyName,
            SuccessRate = best.SuccessRate,
            TotalAttempts = best.TotalAttempts,
            Successes = best.Successes,
            Failures = best.Failures
        };
    }

    /// <summary>
    /// Get full learning history for API exposure / dashboard.
    /// </summary>
    public LearningReport GetReport()
    {
        var patterns = new List<PatternSummary>();

        foreach (var (rootCause, strategies) in _history)
        {
            foreach (var (strategyName, record) in strategies)
            {
                patterns.Add(new PatternSummary
                {
                    RootCause = rootCause,
                    StrategyName = strategyName,
                    Successes = record.Successes,
                    Failures = record.Failures,
                    SuccessRate = record.SuccessRate,
                    TotalAttempts = record.TotalAttempts
                });
            }
        }

        return new LearningReport
        {
            GeneratedAt = DateTime.UtcNow,
            TotalPatterns = patterns.Count,
            Patterns = patterns.OrderByDescending(p => p.TotalAttempts).ToList(),
            RecentEvents = _events.ToArray().Reverse().Take(50).ToList()
        };
    }
}

// ── Models ──────────────────────────────────────────────────────────────

public class StrategyRecord
{
    public string StrategyName { get; set; } = "";
    public int Successes;
    public int Failures;
    public int TotalAttempts => Successes + Failures;
    public double SuccessRate => TotalAttempts == 0 ? 0 : (double)Successes / TotalAttempts;
}

public class StrategyRecommendation
{
    public string StrategyName { get; set; } = "";
    public double SuccessRate { get; set; }
    public int TotalAttempts { get; set; }
    public int Successes { get; set; }
    public int Failures { get; set; }
}

public class LearningEvent
{
    public DateTime Timestamp { get; set; }
    public string RootCause { get; set; } = "";
    public string StrategyName { get; set; } = "";
    public bool Success { get; set; }
    public double NewSuccessRate { get; set; }
    public int TotalAttempts { get; set; }
}

public class PatternSummary
{
    public string RootCause { get; set; } = "";
    public string StrategyName { get; set; } = "";
    public int Successes { get; set; }
    public int Failures { get; set; }
    public double SuccessRate { get; set; }
    public int TotalAttempts { get; set; }
}

public class LearningReport
{
    public DateTime GeneratedAt { get; set; }
    public int TotalPatterns { get; set; }
    public List<PatternSummary> Patterns { get; set; } = new();
    public List<LearningEvent> RecentEvents { get; set; } = new();
}
