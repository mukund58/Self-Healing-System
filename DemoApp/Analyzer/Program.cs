using Analyzer;
using Analyzer.Rules;

var builder = WebApplication.CreateBuilder(args);

var prometheusUrl = builder.Configuration.GetValue<string>("Endpoints:PrometheusBaseUrl")
    ?? "http://localhost:9090";

var taskApiUrl = builder.Configuration.GetValue<string>("Endpoints:TaskApiBaseUrl")
    ?? "http://localhost:5186";

builder.Services.AddHttpClient<PrometheusClient>(client =>
{
    client.BaseAddress = new Uri(prometheusUrl);
});

builder.Services.AddHttpClient<MetricIngestClient>(client =>
{
    client.BaseAddress = new Uri(taskApiUrl);
});

builder.Services.AddSingleton<MemoryLeakRule>();
builder.Services.AddSingleton<MonitoringState>();
builder.Services.AddHostedService<MonitoringService>();

var app = builder.Build();

app.MapGet("/status", (MonitoringState state) =>
{
    if (state.LastResult is null)
        return Results.Ok("No verdict yet");

    return Results.Ok(state.LastResult);
});

app.Run();
