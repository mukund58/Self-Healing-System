using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskApi.Data;
using TaskApi.Domain;

namespace TaskApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecoveryActionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public RecoveryActionsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecoveryActionRequest request)
    {
        var entity = new RecoveryAction
        {
            Id = Guid.NewGuid(),
            FailureEventId = request.FailureEventId,
            ActionType = request.ActionType,
            TargetDeployment = request.TargetDeployment,
            Status = "Pending",
            Details = request.Details,
            PerformedAt = DateTime.UtcNow
        };

        _db.RecoveryActions.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(entity);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var entity = await _db.RecoveryActions.FindAsync(id);
        if (entity is null)
            return NotFound();

        entity.Status = request.Status;
        entity.Details = request.Details ?? entity.Details;
        if (request.Status is "Success" or "Failed")
            entity.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(entity);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? failureEventId,
        [FromQuery] int limit = 50)
    {
        var query = _db.RecoveryActions.AsQueryable();

        if (failureEventId.HasValue)
            query = query.Where(a => a.FailureEventId == failureEventId.Value);

        var results = await query
            .OrderByDescending(a => a.PerformedAt)
            .Take(limit)
            .ToListAsync();

        return Ok(results);
    }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? Details { get; set; }
}

public class CreateRecoveryActionRequest
{
    public Guid FailureEventId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string TargetDeployment { get; set; } = string.Empty;
    public string? Details { get; set; }
}
