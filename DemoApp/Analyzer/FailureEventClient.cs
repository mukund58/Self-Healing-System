using System.Net.Http.Json;
using Analyzer.Domain;
using Microsoft.Extensions.Logging;

namespace Analyzer;

public class FailureEventClient
{
    private readonly HttpClient _http;
    private readonly ILogger<FailureEventClient> _logger;

    public FailureEventClient(HttpClient http, ILogger<FailureEventClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<FailureEvent?> CreateAsync(FailureEvent failure, CancellationToken ct)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/failureevents", failure, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Failed to create failure event: {Status} - {Body}",
                    response.StatusCode, body);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<FailureEvent>(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception creating failure event");
            return null;
        }
    }

    public async Task<bool> ResolveAsync(Guid failureEventId, CancellationToken ct)
    {
        var response = await _http.PatchAsync(
            $"/api/failureevents/{failureEventId}/resolve",
            null, ct);
        return response.IsSuccessStatusCode;
    }
}
