using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Domain;

namespace TaskApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly AppDbContext _db;

    public MetricsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> CreateMetrics([FromBody] List<MetricRecord> metrics)
    {
        if (metrics.Count == 0)
            return BadRequest("No metrics provided");

        var now = DateTime.UtcNow;
        var records = metrics.Select(metric => new MetricRecord
        {
            Id = Guid.NewGuid(),
            MetricId = metric.MetricId,
            MetricType = metric.MetricType,
            MetricValue = metric.MetricValue,
            RecordedAt = metric.RecordedAt == default ? now : metric.RecordedAt
        }).ToList();

        _db.MetricRecords.AddRange(records);
        await _db.SaveChangesAsync();

        return Ok(new { inserted = records.Count });
    }

    [HttpGet]
    public async Task<IActionResult> GetMetrics(
        [FromQuery] string? metricId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 200)
    {
        if (limit <= 0)
            return BadRequest("Limit must be positive");

        var query = _db.MetricRecords.AsQueryable();

        if (!string.IsNullOrWhiteSpace(metricId))
            query = query.Where(m => m.MetricId == metricId);

        if (from.HasValue)
            query = query.Where(m => m.RecordedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(m => m.RecordedAt <= to.Value);

        var results = await query
            .OrderByDescending(m => m.RecordedAt)
            .Take(limit)
            .ToListAsync();

        return Ok(results);
    }
}
