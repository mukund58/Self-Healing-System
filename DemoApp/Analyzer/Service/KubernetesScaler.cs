using k8s;
using k8s.Models;

namespace Analyzer.Service;

public class KubernetesScaler
{
    private IKubernetes? _client;
    private readonly ILogger<KubernetesScaler> _logger;
    private readonly string _namespace;
    private bool _initAttempted;

    public KubernetesScaler(ILogger<KubernetesScaler> logger, IConfiguration config)
    {
        _logger = logger;
        _namespace = config.GetValue<string>("Kubernetes:Namespace") ?? "default";
        _initAttempted = false;
    }

    private void EnsureInitialized()
    {
        if (_initAttempted) return;
        _initAttempted = true;

        try
        {
            KubernetesClientConfiguration config;

            if (KubernetesClientConfiguration.IsInCluster())
            {
                config = KubernetesClientConfiguration.InClusterConfig();
                _logger.LogInformation("Kubernetes client initialized via InClusterConfig (namespace={Ns})", _namespace);
            }
            else
            {
                config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
                _logger.LogInformation("Kubernetes client initialized via kubeconfig (namespace={Ns})", _namespace);
            }

            _client = new Kubernetes(config);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize Kubernetes client; K8s operations will be unavailable");
            _client = null;
        }
    }

    /// <summary>
    /// Scale a deployment to the target replica count.
    /// </summary>
    public async Task<ScaleResult> ScaleDeploymentAsync(
        string deploymentName, int targetReplicas, CancellationToken ct)
    {
        EnsureInitialized();
        if (_client is null)
            return ScaleResult.Fail("Not running inside a Kubernetes cluster");

        try
        {
            var scale = await _client.AppsV1.ReadNamespacedDeploymentScaleAsync(
                deploymentName, _namespace, cancellationToken: ct);
            var current = scale.Spec.Replicas ?? 1;

            _logger.LogInformation(
                "Deployment {Name}: current={Current}, target={Target}",
                deploymentName, current, targetReplicas);

            if (current == targetReplicas)
                return ScaleResult.Ok($"Already at {targetReplicas} replicas");

            scale.Spec.Replicas = targetReplicas;
            await _client.AppsV1.ReplaceNamespacedDeploymentScaleAsync(
                scale, deploymentName, _namespace, cancellationToken: ct);

            return ScaleResult.Ok($"Scaled {deploymentName} from {current} to {targetReplicas}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kubernetes scaling error for {Deployment}", deploymentName);
            return ScaleResult.Fail($"Exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Restart a deployment by patching the pod template annotation (rolling restart).
    /// </summary>
    public async Task<ScaleResult> RestartDeploymentAsync(string deploymentName, CancellationToken ct)
    {
        EnsureInitialized();
        if (_client is null)
            return ScaleResult.Fail("Not running inside a Kubernetes cluster");

        try
        {
            var patchBody = $@"
            {{
                ""spec"": {{
                    ""template"": {{
                        ""metadata"": {{
                            ""annotations"": {{
                                ""self-healing/restartedAt"": ""{DateTime.UtcNow:o}""
                            }}
                        }}
                    }}
                }}
            }}";

            var patch = new V1Patch(patchBody, V1Patch.PatchType.MergePatch);
            await _client.AppsV1.PatchNamespacedDeploymentAsync(
                patch, deploymentName, _namespace, cancellationToken: ct);

            _logger.LogInformation("Restarted deployment {Name}", deploymentName);
            return ScaleResult.Ok($"Restarted deployment {deploymentName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kubernetes restart error for {Deployment}", deploymentName);
            return ScaleResult.Fail($"Exception: {ex.Message}");
        }
    }
}

public class ScaleResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";

    public static ScaleResult Ok(string msg) => new() { Success = true, Message = msg };
    public static ScaleResult Fail(string msg) => new() { Success = false, Message = msg };
}
