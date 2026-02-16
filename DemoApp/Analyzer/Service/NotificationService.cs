using Analyzer.Domain;

namespace Analyzer.Service;

public class NotificationService
{
    private readonly HttpClient _http;
    private readonly ILogger<NotificationService> _logger;
    private readonly string? _webhookUrl;

    public NotificationService(HttpClient http, ILogger<NotificationService> logger, IConfiguration config)
    {
        _http = http;
        _logger = logger;
        _webhookUrl = config.GetValue<string>("Notifications:WebhookUrl");
    }

    public async Task NotifyAsync(
        FailureEvent failure,
        string actionType,
        string recoveryStatus,
        string details,
        CancellationToken ct)
    {
        var message = FormatMessage(failure, actionType, recoveryStatus, details);

        // Always log to console + structured log
        var emoji = recoveryStatus == "Success" ? "✅" : "❌";
        Console.WriteLine($"{emoji} [ALERT] {message}");
        _logger.LogWarning("🔔 NOTIFICATION: {Emoji} {FailureType} | Action: {Action} → {Status} | {Details}",
            emoji, failure.FailureType, actionType, recoveryStatus, details);

        // Send webhook if configured
        if (!string.IsNullOrWhiteSpace(_webhookUrl))
        {
            await SendWebhookAsync(failure, actionType, recoveryStatus, details, message, ct);
        }
        else
        {
            _logger.LogInformation("No webhook URL configured, notification logged only");
        }
    }

    private async Task SendWebhookAsync(
        FailureEvent failure,
        string actionType,
        string recoveryStatus,
        string details,
        string message,
        CancellationToken ct)
    {
        try
        {
            var payload = new
            {
                failureType = failure.FailureType,
                severity = failure.Severity,
                description = failure.Description,
                detectedAt = failure.DetectedAt,
                recoveryAction = actionType,
                recoveryStatus = recoveryStatus,
                recoveryDetails = details,
                summary = message,
                timestamp = DateTime.UtcNow
            };

            var response = await _http.PostAsJsonAsync(_webhookUrl, payload, ct);

            if (response.IsSuccessStatusCode)
                _logger.LogInformation("Webhook notification sent successfully");
            else
                _logger.LogWarning("Webhook notification failed: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send webhook notification");
        }
    }

    private static string FormatMessage(
        FailureEvent failure, string actionType, string recoveryStatus, string details)
    {
        return $"Failure: {failure.FailureType} ({failure.Severity}) - {failure.Description} | " +
               $"Action: {actionType} → {recoveryStatus} | {details}";
    }
}
