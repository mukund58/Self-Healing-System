using Analyzer.Domain;

namespace Analyzer;

public class MonitoringState
{
    public FailureEvent? LastResult { get; set; }
    public List<FailureEvent> RecentEvents { get; set; } = new();
    public List<RecoveryResult> RecentRecoveries { get; set; } = new();
}

public class RecoveryResult
{
    public Guid FailureEventId { get; set; }
    public string ActionType { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Details { get; set; }
    public DateTime PerformedAt { get; set; }
}
