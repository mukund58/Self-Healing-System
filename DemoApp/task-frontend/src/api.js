const API_BASE = import.meta.env.VITE_API_URL || "http://localhost:5186";
const ANALYZER_BASE = import.meta.env.VITE_ANALYZER_URL || "http://localhost:5297";

// ─── Tasks ───
export async function getTasks() {
  const res = await fetch(`${API_BASE}/api/tasks`);
  return res.json();
}

export async function addTask(title) {
  await fetch(`${API_BASE}/api/tasks`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ title }),
  });
}

export async function deleteTask(id) {
  await fetch(`${API_BASE}/api/tasks/${id}`, {
    method: "DELETE",
  });
}

// ─── Metrics ───
export async function getMetrics(limit = 50) {
  const res = await fetch(`${API_BASE}/api/metrics?limit=${limit}`);
  return res.json();
}

// ─── Failure Events ───
export async function getFailureEvents(limit = 50) {
  const res = await fetch(`${API_BASE}/api/failureevents?limit=${limit}`);
  return res.json();
}

export async function resolveFailure(id) {
  const res = await fetch(`${API_BASE}/api/failureevents/${id}/resolve`, {
    method: "PATCH",
  });
  return res.json();
}

// ─── Recovery Actions ───
export async function getRecoveryActions(limit = 50) {
  const res = await fetch(`${API_BASE}/api/recoveryactions?limit=${limit}`);
  return res.json();
}

// ─── Health ───
export async function getHealth() {
  const res = await fetch(`${API_BASE}/health`);
  return res.json();
}

// ─── Analyzer Status ───
export async function getAnalyzerStatus() {
  const res = await fetch(`${ANALYZER_BASE}/status`);
  return res.json();
}

export async function getAnalyzerEvents() {
  const res = await fetch(`${ANALYZER_BASE}/events`);
  return res.json();
}

export async function getAnalyzerRecoveries() {
  const res = await fetch(`${ANALYZER_BASE}/recoveries`);
  return res.json();
}

// ─── Learning Loop ───
export async function getLearningReport() {
  const res = await fetch(`${ANALYZER_BASE}/learning`);
  return res.json();
}

export async function getLearningRecommendation(rootCause) {
  const res = await fetch(`${ANALYZER_BASE}/learning/${encodeURIComponent(rootCause)}`);
  return res.json();
}
