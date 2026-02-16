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

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();

// Auto-apply EF migrations at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Use CORS (ORDER MATTERS)
app.UseCors(corsPolicyName);


app.UseRouting();

app.UseHttpMetrics(); // request duration, count, etc.

app.MapControllers();

app.MapHealthChecks("/health");

app.MapMetrics(); // exposes /metrics

app.Run();
