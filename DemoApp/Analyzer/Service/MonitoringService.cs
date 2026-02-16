using Analyzer.Domain;
using Analyzer.Rules;

namespace Analyzer;

public class MonitoringService : BackgroundService
{
    private readonly PrometheusClient _client;
    private readonly MetricIngestClient _ingest;
    private readonly MemoryLeakRule _rule;
    private readonly MonitoringState _state;

    public MonitoringService(
        PrometheusClient client,
        MetricIngestClient ingest,
        MemoryLeakRule rule,
        MonitoringState state)
    {
        _client = client;
        _ingest = ingest;
        _rule = rule;
        _state = state;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var samples = await _client.GetMemorySamplesAsync();

                if (samples.Any())
                {
                    var payload = samples.Select(sample => new MetricRecordDto
                    {
                        MetricId = "system_runtime_working_set",
                        MetricType = "memory",
                        MetricValue = sample.Value,
                        RecordedAt = sample.Timestamp
                    }).ToList();

                    var stored = await _ingest.SendAsync(payload, stoppingToken);
                    if (!stored)
                        Console.WriteLine("Failed to store metric records");
                }

                if (samples.Any())
                {
                    var latest = samples.Last();
                    var result = _rule.Evaluate(latest.Value);

                    if (result != null)
                    {
                        _state.LastResult = result;
                        Console.WriteLine($"⚠ Memory anomaly detected: {result.Description}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Monitoring error: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
