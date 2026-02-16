using Analyzer;
using Analyzer.Rules;
using Analyzer.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var prometheusUrl = builder.Configuration.GetValue<string>("Endpoints:PrometheusBaseUrl")
    ?? "http://localhost:9090";

var taskApiUrl = builder.Configuration.GetValue<string>("Endpoints:TaskApiBaseUrl")
    ?? "http://localhost:5186";

// KubernetesScaler uses InClusterConfig() — no manual URL/token needed

// Prometheus client
builder.Services.AddHttpClient<PrometheusClient>(client =>
{
    client.BaseAddress = new Uri(prometheusUrl);
});

// TaskApi clients (metrics, failure events, recovery actions)
builder.Services.AddHttpClient<MetricIngestClient>(client =>
{
    client.BaseAddress = new Uri(taskApiUrl);
});

builder.Services.AddHttpClient<FailureEventClient>(client =>
{
    client.BaseAddress = new Uri(taskApiUrl);
});

builder.Services.AddHttpClient<RecoveryActionClient>(client =>
{
    client.BaseAddress = new Uri(taskApiUrl);
});

// Kubernetes scaler (uses official client with InClusterConfig)
builder.Services.AddSingleton<KubernetesScaler>();

// Notification service
builder.Services.AddHttpClient<NotificationService>();

// Anomaly detection rules (singletons — they maintain sliding windows)
builder.Services.AddSingleton<MemoryLeakRule>();
builder.Services.AddSingleton<CpuSpikeRule>();
builder.Services.AddSingleton<HighErrorRateRule>();

// Shared state
builder.Services.AddSingleton<MonitoringState>();

// Intelligence layer
builder.Services.AddSingleton<LearningService>();
builder.Services.AddScoped<DiagnosticService>();
builder.Services.AddScoped<RemediationEngine>();

// Legacy orchestrator (kept for backward compatibility)
builder.Services.AddScoped<RecoveryOrchestrator>();

// Background monitoring
builder.Services.AddHostedService<MonitoringService>();

var app = builder.Build();

app.UseCors();

// ── Status & monitoring endpoints ──────────────────────────────────────

app.MapGet("/status", (MonitoringState state) =>
{
    if (state.LastResult is null)
        return Results.Ok("No verdict yet");

    return Results.Ok(state.LastResult);
});

app.MapGet("/events", (MonitoringState state) =>
{
    return Results.Ok(state.RecentEvents.TakeLast(20));
});

app.MapGet("/recoveries", (MonitoringState state) =>
{
    return Results.Ok(state.RecentRecoveries.TakeLast(20));
});

// ── Intelligence endpoints ─────────────────────────────────────────────

app.MapGet("/learning", (LearningService learner) =>
{
    return Results.Ok(learner.GetReport());
});

app.MapGet("/learning/{rootCause}", (string rootCause, LearningService learner) =>
{
    var recommendation = learner.GetBestStrategy(rootCause);
    return recommendation is not null
        ? Results.Ok(recommendation)
        : Results.Ok(new { message = $"No learning data yet for '{rootCause}'" });
});

app.Run();
