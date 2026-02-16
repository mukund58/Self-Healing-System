using Analyzer.Domain;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

namespace Analyzer;

public class PrometheusClient
{
    private readonly HttpClient _http;

    public PrometheusClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<MetricSample>> GetMemorySamplesAsync()
    {
        // var query = "system_runtime_working_set{container=\"taskapi\"}";
        var query = "system_runtime_working_set{job=\"taskapi\"}";

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
