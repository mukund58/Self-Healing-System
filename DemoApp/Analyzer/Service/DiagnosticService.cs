using Analyzer.Domain;
using Microsoft.Extensions.Logging;

namespace Analyzer.Service;

/// <summary>
/// Analyzes metric context when failures are detected to determine root cause.
/// Looks at metrics history, correlates multiple signals, and classifies failure types.
/// </summary>
public class DiagnosticService
{
    private readonly PrometheusClient _prometheus;
    private readonly ILogger<DiagnosticService> _logger;

    public DiagnosticService(PrometheusClient prometheus, ILogger<DiagnosticService> logger)
    {
        _prometheus = prometheus;
        _logger = logger;
    }

    /// <summary>
    /// Build a comprehensive diagnosis from a detected failure event.
    /// Correlates multiple metric signals to determine true root cause.
    /// </summary>
    public async Task<Diagnosis> DiagnoseAsync(FailureEvent failure, CancellationToken ct)
    {
        _logger.LogInformation("🔎 Starting diagnosis for {FailureType}: {Description}",
            failure.FailureType, failure.Description);

        var diagnosis = new Diagnosis
        {
            OriginalFailure = failure,
            DiagnosedAt = DateTime.UtcNow,
            RootCause = failure.FailureType,
            Confidence = 0.5
        };

        try
        {
            // Gather all available metrics for context
            var memoryTask = _prometheus.GetMemorySamplesAsync();
            var cpuTask = _prometheus.GetCpuUsagePercentAsync();
            var errorRateTask = _prometheus.GetErrorRatePercentAsync();
            var requestRateTask = _prometheus.GetRequestRateAsync();

            await Task.WhenAll(memoryTask, cpuTask, errorRateTask, requestRateTask);

            var memorySamples = memoryTask.Result;
            var cpuPercent = cpuTask.Result;
            var errorRate = errorRateTask.Result;
            var requestRate = requestRateTask.Result;

            var currentMemoryMb = memorySamples.Any() ? memorySamples.Last().Value : 0;

            _logger.LogInformation(
                "📊 Diagnostic context: Memory={MemoryMb:F1}MB, CPU={Cpu:F1}%, ErrorRate={Err:F1}%, ReqRate={Req:F1}/s",
                currentMemoryMb, cpuPercent, errorRate, requestRate);

            // Build context
            diagnosis.Metrics["memory_mb"] = currentMemoryMb;
            diagnosis.Metrics["cpu_percent"] = cpuPercent;
            diagnosis.Metrics["error_rate_percent"] = errorRate;
            diagnosis.Metrics["request_rate_per_sec"] = requestRate;

            // Correlate signals to refine root cause
            diagnosis = CorrelateSignals(diagnosis, currentMemoryMb, cpuPercent, errorRate, requestRate);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Diagnosis encountered errors, using original failure type");
            diagnosis.Notes.Add($"Diagnosis partial: {ex.Message}");
        }

        _logger.LogInformation("🔎 Diagnosis complete: RootCause={RootCause}, Severity={Severity}, Confidence={Confidence:F0}%",
            diagnosis.RootCause, diagnosis.Severity, diagnosis.Confidence * 100);

        return diagnosis;
    }

    private Diagnosis CorrelateSignals(
        Diagnosis diagnosis, double memoryMb, double cpuPercent, double errorRate, double requestRate)
    {
        var signals = new List<string>();

        bool highMemory = memoryMb > 200;
        bool highCpu = cpuPercent > 70;
        bool highErrors = errorRate > 10;
        bool highTraffic = requestRate > 50;

        if (highMemory) signals.Add("HighMemory");
        if (highCpu) signals.Add("HighCPU");
        if (highErrors) signals.Add("HighErrorRate");
        if (highTraffic) signals.Add("HighTraffic");

        diagnosis.CorrelatedSignals = signals;

        // --- Root cause classification ---

        // Memory leak + high CPU = resource exhaustion (GC pressure)
        if (highMemory && highCpu && !highTraffic)
        {
            diagnosis.RootCause = "ResourceExhaustion";
            diagnosis.Severity = "Critical";
            diagnosis.Confidence = 0.90;
            diagnosis.Notes.Add("Memory leak causing GC pressure → CPU spike. Restart required.");
            return diagnosis;
        }

        // High traffic + high errors + high CPU = overload
        if (highTraffic && highErrors && highCpu)
        {
            diagnosis.RootCause = "TrafficOverload";
            diagnosis.Severity = "Critical";
            diagnosis.Confidence = 0.85;
            diagnosis.Notes.Add("Traffic spike exceeding capacity. Scale up needed.");
            return diagnosis;
        }

        // High errors without high traffic = application bug
        if (highErrors && !highTraffic)
        {
            diagnosis.RootCause = "ApplicationError";
            diagnosis.Severity = "Warning";
            diagnosis.Confidence = 0.75;
            diagnosis.Notes.Add("Error rate high without traffic spike. Possible application bug — restart may help.");
            return diagnosis;
        }

        // High traffic + high errors but CPU/memory fine = dependency issue
        if (highTraffic && highErrors && !highCpu && !highMemory)
        {
            diagnosis.RootCause = "DependencyFailure";
            diagnosis.Severity = "Warning";
            diagnosis.Confidence = 0.70;
            diagnosis.Notes.Add("Errors under load but resources OK. Likely downstream dependency issue.");
            return diagnosis;
        }

        // Pure memory leak
        if (highMemory && !highCpu)
        {
            diagnosis.RootCause = "MemoryLeakSuspected";
            diagnosis.Severity = "Warning";
            diagnosis.Confidence = 0.80;
            diagnosis.Notes.Add("Memory growing without CPU pressure. Classic memory leak pattern.");
            return diagnosis;
        }

        // Pure CPU spike
        if (highCpu && !highMemory)
        {
            diagnosis.RootCause = "HighCpuUsage";
            diagnosis.Severity = cpuPercent > 95 ? "Critical" : "Warning";
            diagnosis.Confidence = 0.75;
            diagnosis.Notes.Add("CPU spike without memory pressure. Possible compute-heavy workload.");
            return diagnosis;
        }

        // Default: use original failure type
        diagnosis.Confidence = 0.50;
        diagnosis.Notes.Add("No strong signal correlation found. Using original detection.");
        return diagnosis;
    }
}

/// <summary>
/// Rich diagnosis result with root cause, confidence, and metric context.
/// </summary>
public class Diagnosis
{
    public FailureEvent OriginalFailure { get; set; } = null!;
    public string RootCause { get; set; } = "";
    public string Severity { get; set; } = "Warning";
    public double Confidence { get; set; }
    public DateTime DiagnosedAt { get; set; }
    public Dictionary<string, double> Metrics { get; set; } = new();
    public List<string> CorrelatedSignals { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}
