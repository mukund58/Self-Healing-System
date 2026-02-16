using System.Net.Http.Json;

namespace Analyzer;

public class MetricIngestClient
{
    private readonly HttpClient _http;

    public MetricIngestClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<bool> SendAsync(List<MetricRecordDto> records, CancellationToken cancellationToken)
    {
        if (records.Count == 0)
            return true;

        var response = await _http.PostAsJsonAsync("/api/metrics", records, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}

public class MetricRecordDto
{
    public string MetricId { get; set; } = string.Empty;
    public string MetricType { get; set; } = string.Empty;
    public double MetricValue { get; set; }
    public DateTime RecordedAt { get; set; }
}
