using System.Net.Http.Json;

namespace Analyzer;

public class RecoveryActionClient
{
    private readonly HttpClient _http;

    public RecoveryActionClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<RecoveryActionDto?> CreateAsync(RecoveryActionDto action, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync("/api/recoveryactions", action, ct);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<RecoveryActionDto>(ct);
    }

    public async Task<bool> UpdateStatusAsync(Guid actionId, string status, string? details, CancellationToken ct)
    {
        var response = await _http.PatchAsJsonAsync(
            $"/api/recoveryactions/{actionId}/status",
            new { Status = status, Details = details }, ct);
        return response.IsSuccessStatusCode;
    }
}

public class RecoveryActionDto
{
    public Guid? Id { get; set; }
    public Guid FailureEventId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string TargetDeployment { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? Details { get; set; }
}
