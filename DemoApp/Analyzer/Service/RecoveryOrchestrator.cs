using Analyzer.Domain;

namespace Analyzer.Service;

public class RecoveryOrchestrator
{
    private readonly KubernetesScaler _scaler;
    private readonly FailureEventClient _failureClient;
    private readonly RecoveryActionClient _recoveryClient;
    private readonly NotificationService _notifier;
    private readonly ILogger<RecoveryOrchestrator> _logger;
    private readonly IConfiguration _config;

    public RecoveryOrchestrator(
        KubernetesScaler scaler,
        FailureEventClient failureClient,
        RecoveryActionClient recoveryClient,
        NotificationService notifier,
        ILogger<RecoveryOrchestrator> logger,
        IConfiguration config)
    {
        _scaler = scaler;
        _failureClient = failureClient;
        _recoveryClient = recoveryClient;
        _notifier = notifier;
        _logger = logger;
        _config = config;
    }

    public async Task<RecoveryResult?> HandleFailureAsync(FailureEvent failure, CancellationToken ct)
    {
        // 1. Persist the failure event
        var persisted = await _failureClient.CreateAsync(failure, ct);
        if (persisted?.Id is null)
        {
            _logger.LogError("Failed to persist failure event: {Type}", failure.FailureType);
            return null;
        }

        var failureId = persisted.Id.Value;
        _logger.LogWarning("Failure event {Id}: {Type} - {Desc}",
            failureId, failure.FailureType, failure.Description);

        // 2. Determine recovery action based on failure type
        var (actionType, targetDeployment) = DetermineAction(failure);

        // 3. Create recovery action record
        var actionRecord = await _recoveryClient.CreateAsync(new RecoveryActionDto
        {
            FailureEventId = failureId,
            ActionType = actionType,
            TargetDeployment = targetDeployment,
            Status = "InProgress"
        }, ct);

        // 4. Execute the recovery
        var scaleResult = await ExecuteRecoveryAsync(actionType, targetDeployment, ct);

        // 5. Update action status
        var finalStatus = scaleResult.Success ? "Success" : "Failed";
        if (actionRecord?.Id is not null)
        {
            await _recoveryClient.UpdateStatusAsync(
                actionRecord.Id.Value, finalStatus, scaleResult.Message, ct);
        }

        // 6. Mark failure as resolved if recovery succeeded
        if (scaleResult.Success)
        {
            await _failureClient.ResolveAsync(failureId, ct);
        }

        // 7. Notify admin
        await _notifier.NotifyAsync(failure, actionType, finalStatus, scaleResult.Message, ct);

        _logger.LogInformation("Recovery {Status}: {Action} on {Deployment} - {Message}",
            finalStatus, actionType, targetDeployment, scaleResult.Message);

        return new RecoveryResult
        {
            FailureEventId = failureId,
            ActionType = actionType,
            Status = finalStatus,
            Details = scaleResult.Message,
            PerformedAt = DateTime.UtcNow
        };
    }

    private (string actionType, string targetDeployment) DetermineAction(FailureEvent failure)
    {
        var defaultDeployment = _config.GetValue<string>("Kubernetes:TargetDeployment") ?? "taskapi";

        return failure.FailureType switch
        {
            "MemoryLeakSuspected" => ("RestartPod", defaultDeployment),
            "HighCpuUsage" => ("ScaleUp", defaultDeployment),
            "PodCrashLoop" => ("RestartPod", defaultDeployment),
            "HighLatency" => ("ScaleUp", defaultDeployment),
            _ => ("RestartPod", defaultDeployment)
        };
    }

    private async Task<ScaleResult> ExecuteRecoveryAsync(
        string actionType, string deployment, CancellationToken ct)
    {
        var scaleTarget = _config.GetValue<int>("Kubernetes:ScaleUpReplicas");
        if (scaleTarget <= 0) scaleTarget = 3;

        return actionType switch
        {
            "ScaleUp" => await _scaler.ScaleDeploymentAsync(deployment, scaleTarget, ct),
            "ScaleDown" => await _scaler.ScaleDeploymentAsync(deployment, 1, ct),
            "RestartPod" => await _scaler.RestartDeploymentAsync(deployment, ct),
            _ => ScaleResult.Fail($"Unknown action type: {actionType}")
        };
    }
}
