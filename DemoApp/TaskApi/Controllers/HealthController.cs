using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace TaskApi.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Health()
    {
        var process = Process.GetCurrentProcess();

        var memoryMb = process.WorkingSet64 / (1024 * 1024);
        var threadCount = process.Threads.Count;

        if (memoryMb > 500)
            return StatusCode(503, $"Unhealthy: High memory usage {memoryMb} MB");

        if (threadCount > 200)
            return StatusCode(503, $"Unhealthy: Too many threads {threadCount}");

        return Ok(new
        {
            status = "OK",
            memoryMb,
            threadCount
        });
    }
}
