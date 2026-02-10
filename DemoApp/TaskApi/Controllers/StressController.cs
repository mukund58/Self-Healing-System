using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Prometheus;
namespace TaskApi.Controllers;

[ApiController]
[Route("stress")]
public class StressController : ControllerBase
{

    public static class AppMetrics
    {
        public static readonly Counter CpuStressRequests =
            Metrics.CreateCounter("cpu_stress_requests_total", "CPU stress calls");

        public static readonly Gauge MemoryAllocatedMb =
            Metrics.CreateGauge("memory_allocated_mb", "Memory allocated via endpoint");
    }

    [HttpGet("cpu")]
    public IActionResult Cpu([FromQuery] int seconds = 10)
    {
        AppMetrics.CpuStressRequests.Inc();

        var sw = Stopwatch.StartNew();
        double x = 0;

        while (sw.Elapsed.TotalSeconds < seconds)
        {
            x += Math.Sqrt(Random.Shared.NextDouble());
        }

        return Ok($"CPU stressed for {seconds} seconds");
    }

    [HttpGet("memory")]
    public IActionResult Memory([FromQuery] int mb = 100)
    {
        AppMetrics.MemoryAllocatedMb.Set(mb);

        var buffer = new byte[mb * 1024 * 1024];
        return Ok($"Allocated {mb} MB of memory");
    }

}
