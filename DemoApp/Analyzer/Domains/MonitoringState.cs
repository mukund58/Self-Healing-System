using Analyzer.Domain;

namespace Analyzer;

public class MonitoringState
{
    public FailureEvent? LastResult { get; set; }
}
