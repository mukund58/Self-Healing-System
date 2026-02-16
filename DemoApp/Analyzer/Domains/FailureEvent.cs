namespace Analyzer.Domain;

public class FailureEvent
{
    public string FailureType { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}
