namespace Analyzer.Domain;

public class FailureEvent
{
    public Guid? Id { get; set; }
    public string FailureType { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public bool Resolved { get; set; } = false;
}
