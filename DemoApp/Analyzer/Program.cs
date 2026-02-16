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

// Core services
builder.Services.AddSingleton<MemoryLeakRule>();
builder.Services.AddSingleton<MonitoringState>();
builder.Services.AddScoped<RecoveryOrchestrator>();
builder.Services.AddHostedService<MonitoringService>();

var app = builder.Build();

app.UseCors();

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

app.Run();
