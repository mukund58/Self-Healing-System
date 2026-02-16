namespace TaskApi.Domain;

public class FailureEvent
{
    public Guid Id { get; set; }
    public string FailureType { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public bool Resolved { get; set; } = false;
    public DateTime? ResolvedAt { get; set; }
}
