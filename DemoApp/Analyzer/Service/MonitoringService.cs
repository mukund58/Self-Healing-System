using Analyzer.Domain;
using Analyzer.Rules;
using Analyzer.Service;

namespace Analyzer;

public class MonitoringService : BackgroundService
{
    private readonly PrometheusClient _client;
    private readonly MetricIngestClient _ingest;
    private readonly MemoryLeakRule _memoryRule;
    private readonly CpuSpikeRule _cpuRule;
    private readonly HighErrorRateRule _errorRule;
    private readonly MonitoringState _state;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonitoringService> _logger;

    public MonitoringService(
        PrometheusClient client,
        MetricIngestClient ingest,
        MemoryLeakRule memoryRule,
        CpuSpikeRule cpuRule,
        HighErrorRateRule errorRule,
        MonitoringState state,
        IServiceScopeFactory scopeFactory,
        ILogger<MonitoringService> logger)
    {
        _client = client;
        _ingest = ingest;
        _memoryRule = memoryRule;
        _cpuRule = cpuRule;
        _errorRule = errorRule;
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
                // Fetch all metrics in parallel
                var memorySamplesTask = _client.GetMemorySamplesAsync();
                var cpuTask = _client.GetCpuUsagePercentAsync();
                var errorRateTask = _client.GetErrorRatePercentAsync();
                var requestRateTask = _client.GetRequestRateAsync();

                await Task.WhenAll(memorySamplesTask, cpuTask, errorRateTask, requestRateTask);

                var memorySamples = await memorySamplesTask;
                var cpuPercent = await cpuTask;
                var errorRate = await errorRateTask;
                var requestRate = await requestRateTask;

                // Store all metrics in DB
                var metricsPayload = new List<MetricRecordDto>();

                if (memorySamples.Any())
                {
                    metricsPayload.AddRange(memorySamples.Select(s => new MetricRecordDto
                    {
                        MetricId = "system_runtime_working_set",
                        MetricType = "memory",
                        MetricValue = s.Value,
                        RecordedAt = s.Timestamp
                    }));
                }

                metricsPayload.Add(new MetricRecordDto
                {
                    MetricId = "process_cpu_percent",
                    MetricType = "cpu",
                    MetricValue = cpuPercent,
                    RecordedAt = DateTime.UtcNow
                });

                metricsPayload.Add(new MetricRecordDto
                {
                    MetricId = "http_error_rate_percent",
                    MetricType = "error_rate",
                    MetricValue = errorRate,
                    RecordedAt = DateTime.UtcNow
                });

                metricsPayload.Add(new MetricRecordDto
                {
                    MetricId = "http_request_rate",
                    MetricType = "request_rate",
                    MetricValue = requestRate,
                    RecordedAt = DateTime.UtcNow
                });

                if (metricsPayload.Any())
                {
                    var stored = await _ingest.SendAsync(metricsPayload, stoppingToken);
                    if (!stored)
                        _logger.LogWarning("Failed to store metric records");
                }

                // ── Evaluate all anomaly rules ──────────────────────────────

                // 1. Memory leak rule
                if (memorySamples.Any())
                {
                    var memResult = _memoryRule.Evaluate(memorySamples.Last().Value);
                    if (memResult != null)
                    {
                        _logger.LogWarning("⚠ Memory anomaly: {Desc}", memResult.Description);
                        await TriggerRecoveryAsync(memResult, stoppingToken);
                    }
                }

                // 2. CPU spike rule
                var cpuResult = _cpuRule.Evaluate(cpuPercent);
                if (cpuResult != null)
                {
                    _logger.LogWarning("⚠ CPU anomaly: {Desc}", cpuResult.Description);
                    await TriggerRecoveryAsync(cpuResult, stoppingToken);
                }

                // 3. Error rate rule
                var errorResult = _errorRule.Evaluate(errorRate);
                if (errorResult != null)
                {
                    _logger.LogWarning("⚠ Error rate anomaly: {Desc}", errorResult.Description);
                    await TriggerRecoveryAsync(errorResult, stoppingToken);
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
            var engine = scope.ServiceProvider.GetRequiredService<RemediationEngine>();
            var recoveryResult = await engine.HandleFailureAsync(failure, ct);

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
