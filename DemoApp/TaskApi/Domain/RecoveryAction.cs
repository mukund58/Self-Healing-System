namespace TaskApi.Domain;

public class RecoveryAction
{
    public Guid Id { get; set; }
    public Guid FailureEventId { get; set; }
    public string ActionType { get; set; } = string.Empty;   // e.g. "ScaleUp", "RestartPod"
    public string TargetDeployment { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";          // Pending, InProgress, Success, Failed
    public string? Details { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
