const API_BASE = import.meta.env.VITE_API_URL || "http://localhost:5186";

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
