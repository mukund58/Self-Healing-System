using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:80");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
});
builder.Services.AddControllers();

builder.Services.AddHealthChecks()
    .AddCheck("self", () =>
        HealthCheckResult.Healthy("OK"));


// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:5173") // Vite default
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});


var app = builder.Build();
// Use CORS (ORDER MATTERS)
app.UseCors("AllowFrontend");


app.UseRouting();

app.UseHttpMetrics(); // request duration, count, etc.

app.MapControllers();

app.MapHealthChecks("/health");

app.MapMetrics(); // exposes /metrics

app.Run();
