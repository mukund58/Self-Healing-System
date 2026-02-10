using System;

namespace TaskApi.Domain;

public class MetricRecord
{
    public string MetricId { get; set; } = string.Empty;
    public string MetricType { get; set; } = string.Empty;
    public double MetricValue { get; set; }
    public DateTime RecordedAt { get; set; }
}
