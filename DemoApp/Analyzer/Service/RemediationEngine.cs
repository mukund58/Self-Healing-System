using Analyzer.Domain;

namespace Analyzer.Service;

/// <summary>
/// Strategic remediation engine that selects multi-step recovery strategies
/// based on root cause diagnosis and historical success rates from the learning loop.
/// </summary>
public class RemediationEngine
{
    private readonly KubernetesScaler _scaler;
    private readonly FailureEventClient _failureClient;
    private readonly RecoveryActionClient _recoveryClient;
    private readonly NotificationService _notifier;
    private readonly DiagnosticService _diagnostics;
    private readonly LearningService _learner;
    private readonly ILogger<RemediationEngine> _logger;
    private readonly IConfiguration _config;

    public RemediationEngine(
        KubernetesScaler scaler,
        FailureEventClient failureClient,
        RecoveryActionClient recoveryClient,
        NotificationService notifier,
        DiagnosticService diagnostics,
        LearningService learner,
        ILogger<RemediationEngine> logger,
        IConfiguration config)
    {
        _scaler = scaler;
        _failureClient = failureClient;
        _recoveryClient = recoveryClient;
        _notifier = notifier;
        _diagnostics = diagnostics;
        _learner = learner;
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Full self-healing pipeline: persist failure → diagnose root cause →
    /// select strategy (learning-informed) → execute multi-step recovery →
    /// record outcome for learning loop.
    /// </summary>
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
        _logger.LogWarning("🔴 Failure detected [{Id}]: {Type} ({Severity}) - {Desc}",
            failureId, failure.FailureType, failure.Severity, failure.Description);

        // 2. Diagnose root cause (correlate multiple signals)
        var diagnosis = await _diagnostics.DiagnoseAsync(failure, ct);
        _logger.LogInformation(
            "🔎 Diagnosis: {RootCause} (confidence {Confidence:P0}), signals: [{Signals}]",
            diagnosis.RootCause, diagnosis.Confidence,
            string.Join(", ", diagnosis.CorrelatedSignals));

        foreach (var note in diagnosis.Notes)
            _logger.LogInformation("   📝 {Note}", note);

        // 3. Select recovery strategy (learning-informed)
        var strategy = SelectStrategy(diagnosis);
        _logger.LogInformation("🤖 Selected strategy: {Strategy} ({StepCount} steps)",
            strategy.Name, strategy.Steps.Count);

        // 4. Execute each step in the strategy
        var overallSuccess = true;
        var stepResults = new List<string>();

        foreach (var step in strategy.Steps)
        {
            _logger.LogInformation("   ▶ Executing step: {Action} on {Target}", step.Action, step.Target);

            // Create recovery action record
            var actionRecord = await _recoveryClient.CreateAsync(new RecoveryActionDto
            {
                FailureEventId = failureId,
                ActionType = step.Action,
                TargetDeployment = step.Target,
                Status = "InProgress"
            }, ct);

            var result = await ExecuteStepAsync(step, ct);
            var status = result.Success ? "Success" : "Failed";

            if (actionRecord?.Id is not null)
                await _recoveryClient.UpdateStatusAsync(actionRecord.Id.Value, status, result.Message, ct);

            stepResults.Add($"{step.Action}: {status} - {result.Message}");

            if (!result.Success)
            {
                _logger.LogWarning("   ⚠ Step failed: {Action} - {Message}", step.Action, result.Message);
                overallSuccess = false;

                if (!step.ContinueOnFailure)
                {
                    _logger.LogWarning("   🛑 Strategy aborted (step not marked continue-on-failure)");
                    break;
                }
            }
            else
            {
                _logger.LogInformation("   ✅ Step succeeded: {Action}", step.Action);
            }

            // Short delay between steps to let things settle
            if (step.DelayAfterMs > 0)
                await Task.Delay(step.DelayAfterMs, ct);
        }

        // 5. Mark failure resolved if overall success
        if (overallSuccess)
            await _failureClient.ResolveAsync(failureId, ct);

        var finalStatus = overallSuccess ? "Success" : "PartialFailure";
        var details = string.Join(" | ", stepResults);

        // 6. Record outcome in learning loop
        _learner.RecordOutcome(diagnosis.RootCause, strategy.Name, overallSuccess);

        // 7. Notify
        await _notifier.NotifyAsync(failure, strategy.Name, finalStatus, details, ct);

        _logger.LogInformation("📊 Recovery {Status} for [{RootCause}] via strategy [{Strategy}]: {Details}",
            finalStatus, diagnosis.RootCause, strategy.Name, details);

        return new RecoveryResult
        {
            FailureEventId = failureId,
            ActionType = strategy.Name,
            Status = finalStatus,
            Details = details,
            PerformedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Selects best strategy based on root cause + learning history.
    /// The learning loop may override the default if a different strategy has
    /// a proven higher success rate.
    /// </summary>
    private RemediationStrategy SelectStrategy(Diagnosis diagnosis)
    {
        var deployment = _config.GetValue<string>("Kubernetes:TargetDeployment") ?? "taskapi";
        var scaleTarget = _config.GetValue<int>("Kubernetes:ScaleUpReplicas");
        if (scaleTarget <= 0) scaleTarget = 3;

        // Check if the learning loop has a better strategy recommendation
        var learnedPreference = _learner.GetBestStrategy(diagnosis.RootCause);

        // If learning suggests something with good confidence, prefer it
        if (learnedPreference is not null)
        {
            _logger.LogInformation("📚 Learning loop recommends [{Strategy}] for {RootCause} " +
                "(success rate: {Rate:P0}, samples: {Count})",
                learnedPreference.StrategyName, diagnosis.RootCause,
                learnedPreference.SuccessRate, learnedPreference.TotalAttempts);

            // Use learned preference if success rate > 70% and enough data
            if (learnedPreference.SuccessRate > 0.7 && learnedPreference.TotalAttempts >= 3)
            {
                var learnedStrategy = BuildStrategyByName(learnedPreference.StrategyName, deployment, scaleTarget);
                if (learnedStrategy is not null) return learnedStrategy;
            }
        }

        // Default: select strategy based on root cause
        return diagnosis.RootCause switch
        {
            "ResourceExhaustion" => new RemediationStrategy
            {
                Name = "RestartAndScale",
                Description = "Restart to clear leaked resources, then scale up for resilience",
                Steps = new List<RemediationStep>
                {
                    new() { Action = "RestartPod", Target = deployment, DelayAfterMs = 5000 },
                    new() { Action = "ScaleUp", Target = deployment, Replicas = scaleTarget, ContinueOnFailure = true }
                }
            },

            "TrafficOverload" => new RemediationStrategy
            {
                Name = "ScaleUpAggressive",
                Description = "Scale up immediately to absorb traffic spike",
                Steps = new List<RemediationStep>
                {
                    new() { Action = "ScaleUp", Target = deployment, Replicas = scaleTarget }
                }
            },

            "ApplicationError" => new RemediationStrategy
            {
                Name = "RestartAndMonitor",
                Description = "Restart pods to clear potential transient state corruption",
                Steps = new List<RemediationStep>
                {
                    new() { Action = "RestartPod", Target = deployment, DelayAfterMs = 3000 }
                }
            },

            "DependencyFailure" => new RemediationStrategy
            {
                Name = "ScaleAndRestart",
                Description = "Scale up to spread load, then rolling restart to re-establish connections",
                Steps = new List<RemediationStep>
                {
                    new() { Action = "ScaleUp", Target = deployment, Replicas = scaleTarget, DelayAfterMs = 3000 },
                    new() { Action = "RestartPod", Target = deployment, ContinueOnFailure = true }
                }
            },

            "MemoryLeakSuspected" => new RemediationStrategy
            {
                Name = "RestartWithBuffer",
                Description = "Restart pods to clear leaked memory, scale up to maintain availability",
                Steps = new List<RemediationStep>
                {
                    new() { Action = "ScaleUp", Target = deployment, Replicas = scaleTarget, DelayAfterMs = 3000 },
                    new() { Action = "RestartPod", Target = deployment }
                }
            },

            "HighCpuUsage" => new RemediationStrategy
            {
                Name = "ScaleUpForCpu",
                Description = "Scale up to distribute CPU load across more replicas",
                Steps = new List<RemediationStep>
                {
                    new() { Action = "ScaleUp", Target = deployment, Replicas = scaleTarget }
                }
            },

            _ => new RemediationStrategy
            {
                Name = "DefaultRestart",
                Description = "Default: restart pods as a general recovery action",
                Steps = new List<RemediationStep>
                {
                    new() { Action = "RestartPod", Target = deployment }
                }
            }
        };
    }

    private RemediationStrategy? BuildStrategyByName(string name, string deployment, int scaleTarget)
    {
        return name switch
        {
            "RestartAndScale" => new RemediationStrategy
            {
                Name = "RestartAndScale",
                Description = "Learned: restart then scale",
                Steps = new()
                {
                    new() { Action = "RestartPod", Target = deployment, DelayAfterMs = 5000 },
                    new() { Action = "ScaleUp", Target = deployment, Replicas = scaleTarget, ContinueOnFailure = true }
                }
            },
            "ScaleUpAggressive" => new RemediationStrategy
            {
                Name = "ScaleUpAggressive",
                Description = "Learned: aggressive scale",
                Steps = new()
                {
                    new() { Action = "ScaleUp", Target = deployment, Replicas = scaleTarget }
                }
            },
            "RestartAndMonitor" => new RemediationStrategy
            {
                Name = "RestartAndMonitor",
                Description = "Learned: restart pods",
                Steps = new() { new() { Action = "RestartPod", Target = deployment } }
            },
            "ScaleAndRestart" => new RemediationStrategy
            {
                Name = "ScaleAndRestart",
                Description = "Learned: scale then restart",
                Steps = new()
                {
                    new() { Action = "ScaleUp", Target = deployment, Replicas = scaleTarget, DelayAfterMs = 3000 },
                    new() { Action = "RestartPod", Target = deployment, ContinueOnFailure = true }
                }
            },
            "RestartWithBuffer" => new RemediationStrategy
            {
                Name = "RestartWithBuffer",
                Description = "Learned: scale up buffer then restart",
                Steps = new()
                {
                    new() { Action = "ScaleUp", Target = deployment, Replicas = scaleTarget, DelayAfterMs = 3000 },
                    new() { Action = "RestartPod", Target = deployment }
                }
            },
            "ScaleUpForCpu" => new RemediationStrategy
            {
                Name = "ScaleUpForCpu",
                Description = "Learned: scale for CPU",
                Steps = new() { new() { Action = "ScaleUp", Target = deployment, Replicas = scaleTarget } }
            },
            "DefaultRestart" => new RemediationStrategy
            {
                Name = "DefaultRestart",
                Description = "Learned: default restart",
                Steps = new() { new() { Action = "RestartPod", Target = deployment } }
            },
            _ => null
        };
    }

    private async Task<ScaleResult> ExecuteStepAsync(RemediationStep step, CancellationToken ct)
    {
        try
        {
            return step.Action switch
            {
                "ScaleUp" => await _scaler.ScaleDeploymentAsync(step.Target, step.Replicas, ct),
                "ScaleDown" => await _scaler.ScaleDeploymentAsync(step.Target, 1, ct),
                "RestartPod" => await _scaler.RestartDeploymentAsync(step.Target, ct),
                _ => ScaleResult.Fail($"Unknown action: {step.Action}")
            };
        }
        catch (Exception ex)
        {
            return ScaleResult.Fail($"Exception executing {step.Action}: {ex.Message}");
        }
    }
}

// ── Models ──────────────────────────────────────────────────────────────

public class RemediationStrategy
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<RemediationStep> Steps { get; set; } = new();
}

public class RemediationStep
{
    public string Action { get; set; } = "";     // ScaleUp, ScaleDown, RestartPod
    public string Target { get; set; } = "";     // Deployment name
    public int Replicas { get; set; } = 3;       // For ScaleUp/ScaleDown
    public int DelayAfterMs { get; set; } = 0;   // Pause after step
    public bool ContinueOnFailure { get; set; } = false;
}
