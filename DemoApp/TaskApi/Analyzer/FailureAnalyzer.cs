using System;
using System.Collections.Generic;
using System.Linq;
using TaskApi.Domain;

namespace TaskApi.Analyzer;

/// <summary>
/// Naive analyzer that inspects recent metrics and emits a FailureEvent.
/// Refine thresholds or plug into Prometheus client metrics in future steps.
/// </summary>
public class FailureAnalyzer
{
    private const double MemoryMbCritical = 500.0;
    private const double CpuUsageCritical = 0.90; // 90% normalized usage

    public FailureEvent Analyze(List<MetricRecord> metrics)
    {
        if (metrics == null || metrics.Count == 0)
        {
            return new FailureEvent
            {
                FailureType = "no_data",
                Severity = "info",
                Message = "No metrics available"
            };
        }

        var latestByType = metrics
            .GroupBy(m => m.MetricType, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(m => m.RecordedAt).First())
            .ToDictionary(m => m.MetricType, m => m, StringComparer.OrdinalIgnoreCase);

        if (latestByType.TryGetValue("memory_working_set_mb", out var mem) && mem.MetricValue >= MemoryMbCritical)
        {
            return new FailureEvent
            {
                FailureType = "memory_pressure",
                Severity = "critical",
                Message = $"Memory working set {mem.MetricValue:F1} MB"
            };
        }

        if (latestByType.TryGetValue("cpu_usage_ratio", out var cpu) && cpu.MetricValue >= CpuUsageCritical)
        {
            return new FailureEvent
            {
                FailureType = "cpu_saturation",
                Severity = "warning",
                Message = $"CPU usage {cpu.MetricValue:P0}"
            };
        }

        return new FailureEvent
        {
            FailureType = "healthy",
            Severity = "info",
            Message = "No failure detected"
        };
    }
}
