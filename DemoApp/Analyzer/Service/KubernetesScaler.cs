using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Analyzer.Service;

public class KubernetesScaler
{
    private readonly HttpClient _http;
    private readonly ILogger<KubernetesScaler> _logger;
    private readonly string _namespace;

    public KubernetesScaler(HttpClient http, ILogger<KubernetesScaler> logger, IConfiguration config)
    {
        _http = http;
        _logger = logger;
        _namespace = config.GetValue<string>("Kubernetes:Namespace") ?? "default";
    }

    /// <summary>
    /// Scale a deployment to the target replica count via the Kubernetes API.
    /// </summary>
    public async Task<ScaleResult> ScaleDeploymentAsync(
        string deploymentName, int targetReplicas, CancellationToken ct)
    {
        try
        {
            var url = $"/apis/apps/v1/namespaces/{_namespace}/deployments/{deploymentName}/scale";

            // GET current scale
            var getResp = await _http.GetAsync(url, ct);
            if (!getResp.IsSuccessStatusCode)
            {
                var body = await getResp.Content.ReadAsStringAsync(ct);
                return ScaleResult.Fail($"GET scale failed ({getResp.StatusCode}): {body}");
            }

            var scaleJson = await getResp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(scaleJson);
            var currentReplicas = doc.RootElement
                .GetProperty("spec")
                .GetProperty("replicas")
                .GetInt32();

            _logger.LogInformation(
                "Deployment {Name}: current={Current}, target={Target}",
                deploymentName, currentReplicas, targetReplicas);

            if (currentReplicas == targetReplicas)
            {
                return ScaleResult.Ok($"Already at {targetReplicas} replicas");
            }

            // PATCH scale
            var patch = new
            {
                spec = new { replicas = targetReplicas }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(patch),
                Encoding.UTF8,
                "application/merge-patch+json");

            var patchResp = await _http.PatchAsync(url, content, ct);
            if (!patchResp.IsSuccessStatusCode)
            {
                var body = await patchResp.Content.ReadAsStringAsync(ct);
                return ScaleResult.Fail($"PATCH scale failed ({patchResp.StatusCode}): {body}");
            }

            return ScaleResult.Ok($"Scaled {deploymentName} from {currentReplicas} to {targetReplicas}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kubernetes scaling error for {Deployment}", deploymentName);
            return ScaleResult.Fail($"Exception: {ex.Message}");
        }
    }

    /// <summary>
    /// Restart a deployment by patching the pod template annotation.
    /// </summary>
    public async Task<ScaleResult> RestartDeploymentAsync(string deploymentName, CancellationToken ct)
    {
        try
        {
            var url = $"/apis/apps/v1/namespaces/{_namespace}/deployments/{deploymentName}";
            var patch = new
            {
                spec = new
                {
                    template = new
                    {
                        metadata = new
                        {
                            annotations = new Dictionary<string, string>
                            {
                                ["self-healing/restartedAt"] = DateTime.UtcNow.ToString("o")
                            }
                        }
                    }
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(patch),
                Encoding.UTF8,
                "application/merge-patch+json");

            var response = await _http.PatchAsync(url, content, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                return ScaleResult.Fail($"Restart failed ({response.StatusCode}): {body}");
            }

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
