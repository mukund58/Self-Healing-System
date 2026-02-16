using Analyzer.Domain;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace Analyzer;

public class PrometheusClient
{
    private readonly HttpClient _http;
    private readonly ILogger<PrometheusClient> _logger;

    public PrometheusClient(HttpClient http, ILogger<PrometheusClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<MetricSample>> GetMemorySamplesAsync()
    {
        var query = "system_runtime_working_set{job=\"taskapi\"}";
        return await QueryInstantAsync(query);
    }

    /// <summary>
    /// Get CPU usage as a percentage (0-100+) using rate of process_cpu_seconds_total over 1m.
    /// </summary>
    public async Task<double> GetCpuUsagePercentAsync()
    {
        try
        {
            var query = "rate(process_cpu_seconds_total{job=\"taskapi\"}[1m]) * 100";
            var samples = await QueryInstantAsync(query);
            return samples.Any() ? samples.Last().Value : 0;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to query CPU metrics");
            return 0;
        }
    }

    /// <summary>
    /// Get HTTP 5xx error rate as a percentage of total requests over 1m.
    /// </summary>
    public async Task<double> GetErrorRatePercentAsync()
    {
        try
        {
            // 5xx errors / total requests * 100
            var query = "(sum(rate(http_requests_received_total{job=\"taskapi\",code=~\"5..\"}[1m])) / sum(rate(http_requests_received_total{job=\"taskapi\"}[1m]))) * 100";
            var samples = await QueryInstantAsync(query);
            var value = samples.Any() ? samples.Last().Value : 0;
            // NaN from Prometheus (0/0) comes as NaN
            return double.IsNaN(value) ? 0 : value;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to query error rate metrics");
            return 0;
        }
    }

    /// <summary>
    /// Get total HTTP request rate per second over 1m.
    /// </summary>
    public async Task<double> GetRequestRateAsync()
    {
        try
        {
            var query = "sum(rate(http_requests_received_total{job=\"taskapi\"}[1m]))";
            var samples = await QueryInstantAsync(query);
            return samples.Any() ? samples.Last().Value : 0;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to query request rate metrics");
            return 0;
        }
    }

    private async Task<List<MetricSample>> QueryInstantAsync(string query)
    {
        var url = $"/api/v1/query?query={Uri.EscapeDataString(query)}";

        var response = await _http.GetStringAsync(url);

        using var doc = JsonDocument.Parse(response);

        var values = doc.RootElement
            .GetProperty("data")
            .GetProperty("result");

        var samples = new List<MetricSample>();

        foreach (var item in values.EnumerateArray())
        {
            var valueArray = item.GetProperty("value");
            var timestampSeconds = valueArray[0].GetDouble();
            var valueRaw = valueArray[1].GetString();

            if (!double.TryParse(valueRaw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
                continue;

            var recordedAt = DateTimeOffset.FromUnixTimeMilliseconds(
                (long)(timestampSeconds * 1000))
                .UtcDateTime;

            samples.Add(new MetricSample
            {
                Value = parsedValue,
                Timestamp = recordedAt
            });
        }

        return samples;
    }
}
