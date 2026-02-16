import { useEffect, useState } from "react";
import {
  getTasks, addTask, deleteTask,
  getFailureEvents, resolveFailure,
  getRecoveryActions,
  getAnalyzerStatus,
  getMetrics,
} from "./api";
import "./App.css";

const GRAFANA_URL = import.meta.env.VITE_GRAFANA_URL || "http://localhost:3000";
const TABS = ["Dashboard", "Tasks", "Failures", "Recoveries", "Metrics"];

export default function App() {
  const [tab, setTab] = useState("Dashboard");

  return (
    <div className="app">
      <header className="header">
        <h1>Self-Healing System</h1>
        <nav className="tabs">
          {TABS.map((t) => (
            <button
              key={t}
              className={`tab ${tab === t ? "active" : ""}`}
              onClick={() => setTab(t)}
            >
              {t}
            </button>
          ))}
        </nav>
      </header>
      <main className="content">
        {tab === "Dashboard" && <DashboardTab />}
        {tab === "Tasks" && <TasksTab />}
        {tab === "Failures" && <FailuresTab />}
        {tab === "Recoveries" && <RecoveriesTab />}
        {tab === "Metrics" && <MetricsTab />}
      </main>
    </div>
  );
}

/* ─── Dashboard ─── */
function DashboardTab() {
  const [status, setStatus] = useState(null);

  useEffect(() => {
    getAnalyzerStatus().then(setStatus).catch(() => setStatus("Unavailable"));
    const interval = setInterval(() => {
      getAnalyzerStatus().then(setStatus).catch(() => setStatus("Unavailable"));
    }, 5000);
    return () => clearInterval(interval);
  }, []);

  return (
    <div>
      <div className="status-card">
        <h3>Analyzer Status</h3>
        <pre className="status-value">
          {typeof status === "string" ? status : JSON.stringify(status, null, 2)}
        </pre>
      </div>

      <h3>Grafana Metrics</h3>
      <iframe
        src={`${GRAFANA_URL}/d/self-healing-overview/self-healing-system?orgId=1&refresh=5s&kiosk`}
        className="grafana-embed"
        title="Grafana Dashboard"
      />
    </div>
  );
}

/* ─── Tasks ─── */
function TasksTab() {
  const [tasks, setTasks] = useState([]);
  const [title, setTitle] = useState("");

  const load = () => getTasks().then(setTasks);
  useEffect(() => { load(); }, []);

  async function handleAdd(e) {
    e.preventDefault();
    if (!title.trim()) return;
    await addTask(title);
    setTitle("");
    load();
  }

  return (
    <div>
      <h2>Tasks</h2>
      <form onSubmit={handleAdd} className="add-form">
        <input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="New task"
        />
        <button type="submit">Add</button>
      </form>
      <table className="data-table">
        <thead>
          <tr><th>Title</th><th>Created</th><th></th></tr>
        </thead>
        <tbody>
          {tasks.map((t) => (
            <tr key={t.id}>
              <td>{t.title}</td>
              <td>{new Date(t.createdAt).toLocaleString()}</td>
              <td><button className="btn-danger" onClick={() => { deleteTask(t.id).then(load); }}>Delete</button></td>
            </tr>
          ))}
        </tbody>
      </table>
      {tasks.length === 0 && <p className="empty">No tasks yet</p>}
    </div>
  );
}

/* ─── Failures ─── */
function FailuresTab() {
  const [events, setEvents] = useState([]);

  const load = () => getFailureEvents().then(setEvents);
  useEffect(() => { load(); const i = setInterval(load, 5000); return () => clearInterval(i); }, []);

  return (
    <div>
      <h2>Failure Events</h2>
      <table className="data-table">
        <thead>
          <tr>
            <th>Type</th><th>Severity</th><th>Description</th>
            <th>Detected</th><th>Resolved</th><th></th>
          </tr>
        </thead>
        <tbody>
          {events.map((e) => (
            <tr key={e.id} className={e.resolved ? "resolved" : "unresolved"}>
              <td><span className="badge">{e.failureType}</span></td>
              <td><span className={`severity ${e.severity?.toLowerCase()}`}>{e.severity}</span></td>
              <td>{e.description}</td>
              <td>{new Date(e.detectedAt).toLocaleString()}</td>
              <td>{e.resolved ? "Yes" : "No"}</td>
              <td>
                {!e.resolved && (
                  <button className="btn-resolve" onClick={() => resolveFailure(e.id).then(load)}>
                    Resolve
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {events.length === 0 && <p className="empty">No failure events</p>}
    </div>
  );
}

/* ─── Recoveries ─── */
function RecoveriesTab() {
  const [actions, setActions] = useState([]);

  const load = () => getRecoveryActions().then(setActions);
  useEffect(() => { load(); const i = setInterval(load, 5000); return () => clearInterval(i); }, []);

  return (
    <div>
      <h2>Recovery Actions</h2>
      <table className="data-table">
        <thead>
          <tr>
            <th>Action</th><th>Target</th><th>Status</th>
            <th>Details</th><th>Performed</th><th>Completed</th>
          </tr>
        </thead>
        <tbody>
          {actions.map((a) => (
            <tr key={a.id}>
              <td><span className="badge">{a.actionType}</span></td>
              <td>{a.targetDeployment}</td>
              <td><span className={`status-pill ${a.status?.toLowerCase()}`}>{a.status}</span></td>
              <td className="details-cell">{a.details}</td>
              <td>{new Date(a.performedAt).toLocaleString()}</td>
              <td>{a.completedAt ? new Date(a.completedAt).toLocaleString() : "—"}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {actions.length === 0 && <p className="empty">No recovery actions</p>}
    </div>
  );
}

/* ─── Metrics ─── */
function MetricsTab() {
  const [metrics, setMetrics] = useState([]);

  const load = () => getMetrics(100).then(setMetrics);
  useEffect(() => { load(); const i = setInterval(load, 5000); return () => clearInterval(i); }, []);

  return (
    <div>
      <h2>Stored Metric Records</h2>
      <table className="data-table">
        <thead>
          <tr><th>Metric ID</th><th>Type</th><th>Value</th><th>Recorded At</th></tr>
        </thead>
        <tbody>
          {metrics.map((m) => (
            <tr key={m.id}>
              <td>{m.metricId}</td>
              <td>{m.metricType}</td>
              <td>{m.metricValue?.toFixed(2)}</td>
              <td>{new Date(m.recordedAt).toLocaleString()}</td>
            </tr>
          ))}
        </tbody>
      </table>
      {metrics.length === 0 && <p className="empty">No metrics stored yet</p>}
    </div>
  );
}
