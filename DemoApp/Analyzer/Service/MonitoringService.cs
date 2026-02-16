using Analyzer.Domain;
using Analyzer.Rules;
using Analyzer.Service;

namespace Analyzer;

public class MonitoringService : BackgroundService
{
    private readonly PrometheusClient _client;
    private readonly MetricIngestClient _ingest;
    private readonly MemoryLeakRule _rule;
    private readonly MonitoringState _state;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonitoringService> _logger;

    public MonitoringService(
        PrometheusClient client,
        MetricIngestClient ingest,
        MemoryLeakRule rule,
        MonitoringState state,
        IServiceScopeFactory scopeFactory,
        ILogger<MonitoringService> logger)
    {
        _client = client;
        _ingest = ingest;
        _rule = rule;
        _state = state;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait briefly for services to initialize
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var samples = await _client.GetMemorySamplesAsync();

                // Store metrics in DB
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
                        _logger.LogWarning("Failed to store metric records");
                }

                // Evaluate anomaly rules
                if (samples.Any())
                {
                    var latest = samples.Last();
                    var result = _rule.Evaluate(latest.Value);

                    if (result != null)
                    {
                        _logger.LogWarning("⚠ Anomaly detected: {Type} - {Desc}",
                            result.FailureType, result.Description);

                        // Trigger recovery orchestrator — updates state with persisted ID
                        await TriggerRecoveryAsync(result, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Monitoring error");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task TriggerRecoveryAsync(FailureEvent failure, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<RecoveryOrchestrator>();
            var recoveryResult = await orchestrator.HandleFailureAsync(failure, ct);

            if (recoveryResult != null)
            {
                // Update state with the persisted failure (which now has a real Id)
                failure.Id = recoveryResult.FailureEventId;
                _state.LastResult = failure;
                _state.RecentEvents.Add(failure);
                _state.RecentRecoveries.Add(recoveryResult);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recovery orchestration failed");
        }
    }
}
