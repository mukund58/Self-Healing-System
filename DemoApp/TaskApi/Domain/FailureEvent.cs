namespace TaskApi.Domain;

public class FailureEvent
{
    public string FailureId { get; set; } = Guid.NewGuid().ToString();
    public string FailureType { get; set; } = string.Empty;
    public string Severity { get; set; } = "info";
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public string? Message { get; set; }
}
