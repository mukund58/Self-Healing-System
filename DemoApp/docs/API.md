# Self-Healing System — API Documentation

> Complete reference for all REST API endpoints across the TaskApi and Analyzer services.

---

## Table of Contents

- [TaskApi Endpoints](#taskapi-endpoints)
  - [Tasks](#tasks)
  - [Failure Events](#failure-events)
  - [Recovery Actions](#recovery-actions)
  - [Metrics](#metrics)
  - [Health Check](#health-check)
  - [Stress Testing](#stress-testing)
  - [Prometheus Metrics](#prometheus-metrics)
- [Analyzer Endpoints](#analyzer-endpoints)
  - [Status](#status)
  - [Events](#events)
  - [Recoveries](#recoveries)
- [Data Models](#data-models)
- [Error Handling](#error-handling)
- [Internal Communication](#internal-communication)

---

## Base URLs

| Service    | Local (port-forward)      | In-Cluster          |
|------------|---------------------------|---------------------|
| TaskApi    | `http://localhost:5186`   | `http://taskapi`    |
| Analyzer   | `http://localhost:5297`   | `http://analyzer`   |
| Prometheus | `http://localhost:9090`   | `http://prometheus:9090` |
| Grafana    | `http://localhost:3000`   | `http://grafana:3000`    |

---

## TaskApi Endpoints

### Tasks

#### `GET /api/tasks`

List all tasks, ordered by creation date (newest first).

**Response:** `200 OK`

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Setup monitoring",
    "createdAt": "2025-01-15T10:30:00Z"
  }
]
```

---

#### `POST /api/tasks`

Create a new task.

**Request Body:**

```json
{
  "title": "My new task"
}
```

**Response:** `201 Created`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "My new task",
  "createdAt": "2025-01-15T10:30:00Z"
}
```

---

#### `DELETE /api/tasks/{id}`

Delete a task by ID.

| Parameter | Type   | Location | Description |
|-----------|--------|----------|-------------|
| `id`      | `guid` | path     | Task UUID   |

**Response:** `204 No Content`

**Error:** `404 Not Found` if task doesn't exist.

---

### Failure Events

#### `GET /api/failureevents`

List failure events with optional filters.

| Parameter     | Type     | Location | Default | Description                          |
|---------------|----------|----------|---------|--------------------------------------|
| `resolved`    | `bool?`  | query    | —       | Filter by resolution status          |
| `failureType` | `string?`| query    | —       | Filter by type (e.g. `MemoryLeakSuspected`) |
| `limit`       | `int`    | query    | `50`    | Max results to return                |

**Response:** `200 OK`

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "failureType": "MemoryLeakSuspected",
    "severity": "Warning",
    "description": "Memory growing at 25.30 MB/min",
    "detectedAt": "2025-01-15T10:30:00Z",
    "resolved": false,
    "resolvedAt": null
  }
]
```

---

#### `POST /api/failureevents`

Create a new failure event.

**Request Body:**

```json
{
  "failureType": "MemoryLeakSuspected",
  "severity": "Warning",
  "description": "Memory growing at 25.30 MB/min",
  "detectedAt": "2025-01-15T10:30:00Z"
}
```

| Field         | Type       | Required | Default          | Description                          |
|---------------|------------|----------|------------------|--------------------------------------|
| `failureType` | `string`   | yes      | —                | Type identifier                      |
| `severity`    | `string`   | no       | `"Info"`         | `Info`, `Warning`, `Critical`        |
| `description` | `string`   | no       | `""`             | Human-readable description           |
| `detectedAt`  | `datetime` | no       | `DateTime.UtcNow`| Detection timestamp                  |

**Response:** `200 OK` — Returns the created entity with generated `id`.

---

#### `PATCH /api/failureevents/{id}/resolve`

Mark a failure event as resolved.

| Parameter | Type   | Location | Description         |
|-----------|--------|----------|---------------------|
| `id`      | `guid` | path     | Failure event UUID  |

**Response:** `200 OK`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "failureType": "MemoryLeakSuspected",
  "severity": "Warning",
  "description": "Memory growing at 25.30 MB/min",
  "detectedAt": "2025-01-15T10:30:00Z",
  "resolved": true,
  "resolvedAt": "2025-01-15T10:35:00Z"
}
```

**Error:** `404 Not Found` if event doesn't exist.

---

### Recovery Actions

#### `GET /api/recoveryactions`

List recovery actions with optional filters.

| Parameter        | Type    | Location | Default | Description                    |
|------------------|---------|----------|---------|--------------------------------|
| `failureEventId` | `guid?` | query    | —       | Filter by failure event        |
| `limit`          | `int`   | query    | `50`    | Max results to return          |

**Response:** `200 OK`

```json
[
  {
    "id": "d2e4f6a8-1234-5678-9abc-def012345678",
    "failureEventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "actionType": "RestartPod",
    "targetDeployment": "taskapi",
    "status": "Success",
    "details": "Restarted pod taskapi-xyz",
    "performedAt": "2025-01-15T10:31:00Z",
    "completedAt": "2025-01-15T10:31:05Z"
  }
]
```

---

#### `POST /api/recoveryactions`

Create a new recovery action.

**Request Body:**

```json
{
  "failureEventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "actionType": "RestartPod",
  "targetDeployment": "taskapi",
  "details": "Scaling up to 3 replicas"
}
```

| Field              | Type     | Required | Default     | Description                        |
|--------------------|----------|----------|-------------|------------------------------------|
| `failureEventId`   | `guid`   | yes      | —           | Associated failure event           |
| `actionType`       | `string` | yes      | —           | `RestartPod`, `ScaleUp`, etc.      |
| `targetDeployment` | `string` | yes      | —           | K8s deployment name                |
| `details`          | `string?`| no       | `null`      | Additional details                 |

**Response:** `200 OK` — Returns entity with `status: "Pending"`, generated `id`, and `performedAt`.

---

#### `PATCH /api/recoveryactions/{id}/status`

Update the status of a recovery action.

| Parameter | Type   | Location | Description          |
|-----------|--------|----------|----------------------|
| `id`      | `guid` | path     | Recovery action UUID |

**Request Body:**

```json
{
  "status": "Success",
  "details": "Pod restarted successfully"
}
```

| Field     | Type      | Required | Description                                       |
|-----------|-----------|----------|---------------------------------------------------|
| `status`  | `string`  | yes      | `Pending`, `InProgress`, `Success`, `Failed`       |
| `details` | `string?` | no       | Updates details (keeps existing if null)           |

> When `status` is `"Success"` or `"Failed"`, `completedAt` is automatically set.

**Response:** `200 OK`

**Error:** `404 Not Found` if action doesn't exist.

---

### Metrics

#### `GET /api/metrics`

List metric records with optional filters.

| Parameter  | Type       | Location | Default | Description                    |
|------------|------------|----------|---------|--------------------------------|
| `metricId` | `string?`  | query    | —       | Filter by metric identifier    |
| `from`     | `datetime?`| query    | —       | Start of time range            |
| `to`       | `datetime?`| query    | —       | End of time range              |
| `limit`    | `int`      | query    | `200`   | Max results to return          |

**Response:** `200 OK`

```json
[
  {
    "id": "a1b2c3d4-5678-9abc-def0-123456789abc",
    "metricId": "system_runtime_working_set",
    "metricType": "memory",
    "metricValue": 128.45,
    "recordedAt": "2025-01-15T10:30:00Z"
  }
]
```

**Error:** `400 Bad Request` if `limit <= 0`.

---

#### `POST /api/metrics`

Batch insert metric records.

**Request Body:** Array of metric records.

```json
[
  {
    "metricId": "system_runtime_working_set",
    "metricType": "memory",
    "metricValue": 128.45,
    "recordedAt": "2025-01-15T10:30:00Z"
  }
]
```

| Field         | Type       | Required | Description                                   |
|---------------|------------|----------|-----------------------------------------------|
| `metricId`    | `string`   | yes      | Metric identifier                             |
| `metricType`  | `string`   | yes      | Type category (`memory`, `cpu`, etc.)         |
| `metricValue` | `double`   | yes      | Numeric value                                 |
| `recordedAt`  | `datetime` | no       | Timestamp (defaults to `DateTime.UtcNow`)     |

**Response:** `200 OK`

```json
{
  "inserted": 5
}
```

**Error:** `400 Bad Request` if array is empty.

---

### Health Check

#### `GET /health`

Application health check with memory and thread diagnostics.

**Response:** `200 OK`

```json
{
  "status": "OK",
  "memoryMb": 85,
  "threadCount": 24
}
```

**Unhealthy Responses:** `503 Service Unavailable`

| Condition              | Response                                 |
|------------------------|------------------------------------------|
| Memory > 500 MB       | `"Unhealthy: High memory usage {x} MB"`  |
| Threads > 200         | `"Unhealthy: Too many threads {x}"`      |

> Also exposed via ASP.NET Health Checks middleware at the same path.

---

### Stress Testing

#### `GET /stress/cpu`

Simulate CPU stress for testing anomaly detection.

| Parameter | Type  | Location | Default | Description          |
|-----------|-------|----------|---------|----------------------|
| `seconds` | `int` | query    | `10`    | Duration in seconds  |

**Response:** `200 OK`

```text
CPU stressed for 10 seconds
```

> Increments `cpu_stress_requests_total` Prometheus counter.

---

#### `GET /stress/memory`

Simulate a memory leak for testing anomaly detection.

| Parameter | Type  | Location | Default | Description          |
|-----------|-------|----------|---------|----------------------|
| `mb`      | `int` | query    | `100`   | Megabytes to leak    |

**Response:** `200 OK`

```text
Leaked 100 MB
```

> Updates `memory_allocated_mb` Prometheus gauge. Memory is intentionally NOT garbage collected.

---

### Prometheus Metrics

#### `GET /metrics`

Standard Prometheus metrics endpoint (exposed via `prometheus-net`).

**Response:** `200 OK` with `text/plain` content type.

Includes:
- `http_request_duration_seconds` — Request duration histogram
- `http_requests_received_total` — Request counter by method/status
- `cpu_stress_requests_total` — CPU stress call counter
- `memory_allocated_mb` — Memory allocated via stress endpoint
- `dotnet_*` — .NET runtime metrics
- `process_*` — Process metrics (CPU, memory, threads)

---

## Analyzer Endpoints

### Status

#### `GET /status`

Get the last analysis result from the monitoring service.

**Response:** `200 OK`

If no analysis has been performed yet:
```json
"No verdict yet"
```

If an anomaly was detected:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "failureType": "MemoryLeakSuspected",
  "severity": "Warning",
  "description": "Memory growing at 25.30 MB/min",
  "detectedAt": "2025-01-15T10:30:00Z",
  "resolved": false
}
```

---

### Events

#### `GET /events`

Get the last 20 failure events detected by the analyzer.

**Response:** `200 OK`

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "failureType": "MemoryLeakSuspected",
    "severity": "Warning",
    "description": "Memory growing at 25.30 MB/min",
    "detectedAt": "2025-01-15T10:30:00Z",
    "resolved": false
  }
]
```

---

### Recoveries

#### `GET /recoveries`

Get the last 20 recovery results from the analyzer.

**Response:** `200 OK`

```json
[
  {
    "failureEventId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "actionType": "RestartPod",
    "status": "Success",
    "details": "Restarted pod taskapi-xyz",
    "performedAt": "2025-01-15T10:31:00Z"
  }
]
```

---

## Data Models

### TaskItem

| Field       | Type       | Description                |
|-------------|------------|----------------------------|
| `id`        | `guid`     | Unique identifier          |
| `title`     | `string`   | Task title                 |
| `createdAt` | `datetime` | Creation timestamp (UTC)   |

### FailureEvent

| Field         | Type        | Description                        |
|---------------|-------------|------------------------------------|
| `id`          | `guid`      | Unique identifier                  |
| `failureType` | `string`   | Type (e.g. `MemoryLeakSuspected`) |
| `severity`    | `string`    | `Info`, `Warning`, `Critical`      |
| `description` | `string`    | Human-readable description         |
| `detectedAt`  | `datetime`  | Detection timestamp (UTC)          |
| `resolved`    | `bool`      | Resolution status                  |
| `resolvedAt`  | `datetime?` | Resolution timestamp               |

### MetricRecord

| Field         | Type       | Description                    |
|---------------|------------|--------------------------------|
| `id`          | `guid`     | Unique identifier              |
| `metricId`    | `string`   | Metric identifier              |
| `metricType`  | `string`   | Type category                  |
| `metricValue` | `double`   | Numeric value                  |
| `recordedAt`  | `datetime` | Recording timestamp (UTC)      |

### RecoveryAction

| Field              | Type        | Description                                    |
|--------------------|-------------|------------------------------------------------|
| `id`               | `guid`      | Unique identifier                              |
| `failureEventId`   | `guid`      | Associated failure event                       |
| `actionType`       | `string`    | `RestartPod`, `ScaleUp`                        |
| `targetDeployment` | `string`    | Kubernetes deployment name                     |
| `status`           | `string`    | `Pending`, `InProgress`, `Success`, `Failed`   |
| `details`          | `string?`   | Additional information                         |
| `performedAt`      | `datetime`  | Action start timestamp (UTC)                   |
| `completedAt`      | `datetime?` | Completion timestamp                           |

### RecoveryResult (Analyzer internal)

| Field            | Type       | Description                |
|------------------|------------|----------------------------|
| `failureEventId` | `guid`    | Associated failure event   |
| `actionType`     | `string`   | Type of recovery           |
| `status`         | `string`   | Outcome status             |
| `details`        | `string?`  | Additional information     |
| `performedAt`    | `datetime` | Action timestamp           |

### MetricSample (Analyzer internal)

| Field       | Type       | Description           |
|-------------|------------|-----------------------|
| `value`     | `double`   | Metric value in MB    |
| `timestamp` | `datetime` | Sample timestamp      |

---

## Error Handling

All endpoints follow standard HTTP status codes:

| Status | Meaning                                           |
|--------|---------------------------------------------------|
| `200`  | Success                                           |
| `201`  | Created (only `POST /api/tasks`)                 |
| `204`  | No Content (only `DELETE /api/tasks/{id}`)       |
| `400`  | Bad Request — invalid input                       |
| `404`  | Not Found — resource doesn't exist                |
| `503`  | Service Unavailable — health check failure        |

Error responses return a plain string or problem details JSON.

---

## Internal Communication

The system has the following internal service communication flow:

```
┌──────────────┐     Prometheus Query API      ┌────────────────┐
│   Analyzer    │ ←─────────────────────────── │   Prometheus    │
│  (Port 8080)  │                               │  (Port 9090)   │
└──────┬───────┘                               └───────┬────────┘
       │                                               │
       │  POST /api/failureevents                      │ Scrape /metrics
       │  POST /api/metrics                            │ every 5s
       │  POST /api/recoveryactions                    │
       │  PATCH /api/recoveryactions/{id}/status       │
       ▼                                               ▼
┌──────────────┐                               ┌────────────────┐
│   TaskApi     │ ─── exposes /metrics ──────→ │   TaskApi Pod   │
│  (Port 8080)  │                               │  (in cluster)  │
└──────────────┘                               └────────────────┘
       │
       │  Kubernetes API
       │  (Scale, Restart, Delete pods)
       ▼
┌──────────────┐
│  Kubernetes   │
│  API Server   │
└──────────────┘
```

### Analyzer → Prometheus
- **Query:** `GET /api/v1/query?query=process_working_set_bytes{job="taskapi"}`
- **Interval:** Every 5 seconds
- **Purpose:** Fetch current memory usage of TaskApi pods

### Analyzer → TaskApi
- `POST /api/metrics` — Store collected metric samples
- `POST /api/failureevents` — Persist detected anomalies
- `POST /api/recoveryactions` — Record recovery actions taken
- `PATCH /api/recoveryactions/{id}/status` — Update action status after execution

### Analyzer → Kubernetes API
- **Scale:** `PATCH /apis/apps/v1/namespaces/default/deployments/taskapi/scale`
- **Restart:** `PATCH /apis/apps/v1/namespaces/default/deployments/taskapi` (annotation update)
- **Delete Pod:** `DELETE /api/v1/namespaces/default/pods/{name}`
- **Auth:** ServiceAccount `analyzer-sa` with RBAC role

### Prometheus → TaskApi
- **Scrape:** `GET /metrics` on `taskapi.default.svc.cluster.local:80`
- **Interval:** Every 5 seconds

---

## Quick Examples

### Create a task
```bash
curl -X POST http://localhost:5186/api/tasks \
  -H "Content-Type: application/json" \
  -d '{"title": "Test task"}'
```

### Trigger memory leak (for testing)
```bash
curl "http://localhost:5186/stress/memory?mb=200"
```

### Check analyzer status
```bash
curl http://localhost:5297/status
```

### Get failure events (unresolved only)
```bash
curl "http://localhost:5186/api/failureevents?resolved=false"
```

### Get recovery actions for a failure
```bash
curl "http://localhost:5186/api/recoveryactions?failureEventId=<guid>"
```

### Get metrics (last hour)
```bash
curl "http://localhost:5186/api/metrics?metricId=system_runtime_working_set&limit=100"
```

### Health check
```bash
curl http://localhost:5186/health
```
