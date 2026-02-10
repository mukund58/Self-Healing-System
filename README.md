# Self-Healing-System
 Self-Healing System – UML Class Diagram

A comprehensive self-healing system designed to automatically detect, diagnose, and recover from failures in distributed systems. This project includes UML class diagrams and ER diagrams to illustrate the system architecture and database design.

## 📋 Overview

The Self-Healing System is an intelligent infrastructure management solution that monitors system health, identifies anomalies, and automatically applies corrective actions to maintain optimal performance and availability.

## 🎯 Features

- **Automated Failure Detection**: Continuous monitoring of system components
- **Self-Diagnosis**: Intelligent analysis of system issues
- **Automatic Recovery**: Predefined and adaptive recovery strategies
- **Scalable Architecture**: Designed for distributed systems
- **Comprehensive Logging**: Detailed event tracking and audit trails

## 📊 System Architecture

**Note:** The UML and ER diagrams represent the planned/future design. The current demo implementation is smaller and focuses on health checks, metrics, and Kubernetes self-healing.

### UML Class Diagram

The system architecture is documented in the UML class diagram, which illustrates the relationships between key components:

### Sequence Diagram.
![Sequence Diagram]( UML/Sequence-Diagram.png)

### Use-Case Diagram
![Use Case  Diagram]( UML/USE-Case-Diagram.png)

### UML-Activity Diagram
![UML Activity Diagram]( UML/UML-Activity-Diagram.png)

### ER Diagrams

The database schema and entity relationships are visualized in multiple ER diagram versions:

#### Primary ER Diagram
![ER Diagram - Main](UML/Er-Diagram.png)

#### Relational Table 
![Relational Table ](UML/Relational-Table.png)

## 📖 Documentation

- Read the [docs](DemoApp/README.md)
- **UML Class Diagram**: Defines the object-oriented structure and relationships
- **ER Diagrams**: Define the database schema and entity relationships

## ✅ How To Use In Your Project

If you want to apply the self-healing approach in your own system, use this demo as a reference and follow these steps.

### 1) Add Health And Metrics Endpoints

- Implement a `/health` endpoint that returns `503` when the service is unhealthy.
- Expose `/metrics` for Prometheus scraping.

Reference files:
- [DemoApp/TaskApi/Controllers/HealthController.cs](DemoApp/TaskApi/Controllers/HealthController.cs)
- [DemoApp/TaskApi/Program.cs](DemoApp/TaskApi/Program.cs)

### 2) Add Stress Or Failure Simulation (Optional)

Use stress endpoints to validate healing behavior during load or failure scenarios.

Reference file:
- [DemoApp/TaskApi/Controllers/StressController.cs](DemoApp/TaskApi/Controllers/StressController.cs)

### 3) Deploy With Kubernetes Probes

- Configure liveness and readiness probes to call `/health`.
- Kubernetes will restart unhealthy pods automatically.

Reference file:
- [DemoApp/TaskApi/Kubernetes/deployment.yaml](DemoApp/TaskApi/Kubernetes/deployment.yaml)

### 4) Add Prometheus Monitoring

- Install Prometheus.
- Add a ServiceMonitor to scrape `/metrics`.

Reference files:
- [DemoApp/TaskApi/Kubernetes/service.yaml](DemoApp/TaskApi/Kubernetes/service.yaml)
- [DemoApp/TaskApi/Kubernetes/service-monitor.yaml](DemoApp/TaskApi/Kubernetes/service-monitor.yaml)
- [DemoApp/TaskApi/Kubernetes/prometheus.yaml](DemoApp/TaskApi/Kubernetes/prometheus.yaml)

### 5) Run Locally (Optional)

Use Docker Compose to run the API and database locally.

Reference file:
- [DemoApp/docker-compose.yml](DemoApp/docker-compose.yml)

### 6) Adapt To Your Service

- Replace the Task API endpoints with your own business logic.
- Keep the health checks and metrics as part of your service boundary.

If you need a full walkthrough, follow [DemoApp/README.md](DemoApp/README.md).

## 📦 Copy-Paste Scaffold Template

If you want a quick starting point, you can scaffold a minimal self-healing setup into your own service and replace the business logic.

### Backend (ASP.NET Core)

**Program.cs (core wiring)**

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks()
		.AddCheck("self", () => HealthCheckResult.Healthy("OK"));

var app = builder.Build();

app.UseRouting();
app.UseHttpMetrics();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();
```

**HealthController.cs**

```csharp
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

				return Ok(new { status = "OK", memoryMb, threadCount });
		}
}
```

### Kubernetes (Probes + Service)

**deployment.yaml (probes only)**

```yaml
containers:
- name: your-service
	image: your-image:tag
	ports:
	- containerPort: 8080
		name: http
	livenessProbe:
		httpGet:
			path: /health
			port: 8080
		initialDelaySeconds: 10
		periodSeconds: 10
	readinessProbe:
		httpGet:
			path: /health
			port: 8080
		initialDelaySeconds: 5
		periodSeconds: 5
```

**service.yaml (for Prometheus scrape)**

```yaml
apiVersion: v1
kind: Service
metadata:
	name: your-service
	labels:
		app: your-service
spec:
	selector:
		app: your-service
	ports:
		- name: http
			port: 80
			targetPort: 8080
```

### Prometheus (ServiceMonitor)

```yaml
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
	name: your-service
	labels:
		app: prometheus
spec:
	selector:
		matchLabels:
			app: your-service
	endpoints:
		- port: http
			path: /metrics
			interval: 5s
```

### Replace With Your Logic

- Keep `/health` and `/metrics` as stable endpoints.
- Swap in your own controllers, DB, and domain models.

If you want the full working example, clone and follow [DemoApp/README.md](DemoApp/README.md).


## 📄 License

This project is licensed under the terms specified in the [LICENSE](LICENSE) file.


- GitHub: [@mukund58](https://github.com/mukund58)
