import { useEffect, useState } from "react";
import { getTasks, addTask, deleteTask } from "./api";

export default function App() {
  const [tasks, setTasks] = useState([]);
  const [title, setTitle] = useState("");

  async function loadTasks() {
    const data = await getTasks();
    setTasks(data);
  }

  useEffect(() => {
    loadTasks();
  }, []);

  async function handleAdd(e) {
    e.preventDefault();
    if (!title.trim()) return;

    await addTask(title);
    setTitle("");
    loadTasks();
  }

  async function handleDelete(id) {
    await deleteTask(id);
    loadTasks();
  }

  return (
    <div style={{ padding: "2rem", maxWidth: "500px" }}>
      <h2>Task Manager</h2>

      <form onSubmit={handleAdd}>
        <input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="New task"
        />
        <button>Add</button>
      </form>

      <ul>
        {tasks.map((t) => (
          <li key={t.id}>
            {t.title}
            <button onClick={() => handleDelete(t.id)}>X</button>
          </li>
        ))}
      </ul>
    </div>
  );
}
