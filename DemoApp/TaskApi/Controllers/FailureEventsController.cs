using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Domain;

namespace TaskApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FailureEventsController : ControllerBase
{
    private readonly AppDbContext _db;

    public FailureEventsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFailureEventRequest request)
    {
        var entity = new FailureEvent
        {
            Id = Guid.NewGuid(),
            FailureType = request.FailureType,
            Severity = request.Severity,
            Description = request.Description,
            DetectedAt = request.DetectedAt == default ? DateTime.UtcNow : request.DetectedAt,
            Resolved = false
        };

        _db.FailureEvents.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(entity);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? resolved,
        [FromQuery] string? failureType,
        [FromQuery] int limit = 50)
    {
        var query = _db.FailureEvents.AsQueryable();

        if (resolved.HasValue)
            query = query.Where(e => e.Resolved == resolved.Value);

        if (!string.IsNullOrWhiteSpace(failureType))
            query = query.Where(e => e.FailureType == failureType);

        var results = await query
            .OrderByDescending(e => e.DetectedAt)
            .Take(limit)
            .ToListAsync();

        return Ok(results);
    }

    [HttpPatch("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id)
    {
        var entity = await _db.FailureEvents.FindAsync(id);
        if (entity is null)
            return NotFound();

        entity.Resolved = true;
        entity.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(entity);
    }
}

public class CreateFailureEventRequest
{
    public string FailureType { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string Description { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
}
