using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;
using TaskApi.Data;

var builder = WebApplication.CreateBuilder(args);
const string corsPolicyName = "AllowFrontend";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
});
builder.Services.AddControllers();

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("OK"));

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();
// Use CORS (ORDER MATTERS)
app.UseCors(corsPolicyName);


app.UseRouting();

app.UseHttpMetrics(); // request duration, count, etc.

app.MapControllers();

app.MapHealthChecks("/health");

app.MapMetrics(); // exposes /metrics

app.Run();
