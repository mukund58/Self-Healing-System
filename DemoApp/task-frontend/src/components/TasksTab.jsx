import { useEffect, useState } from "react";
import { Plus, Trash2, ListTodo, CheckCircle, Calendar } from "lucide-react";
import { getTasks, addTask, deleteTask } from "@/api";
import { cn, timeAgo } from "@/lib/utils";
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Badge } from "@/components/ui/Badge";
import {
  Table, TableHeader, TableBody, TableRow, TableHead, TableCell, TableEmpty,
} from "@/components/ui/Table";
import {
  Dialog, DialogTrigger, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter, DialogClose,
} from "@/components/ui/Dialog";

export function TasksTab() {
  const [tasks, setTasks] = useState([]);
  const [title, setTitle] = useState("");
  const [deleteId, setDeleteId] = useState(null);

  const load = () => getTasks().then(setTasks).catch(() => {});
  useEffect(() => { load(); }, []);

  async function handleAdd(e) {
    e.preventDefault();
    if (!title.trim()) return;
    await addTask(title);
    setTitle("");
    load();
  }

  async function handleDelete() {
    if (!deleteId) return;
    await deleteTask(deleteId);
    setDeleteId(null);
    load();
  }

  return (
    <div className="flex flex-col gap-5 animate-[fade-up_0.4s_ease-out]">
      {/* Header Row */}
      <div className="flex items-center justify-between">
        <div className="flex flex-col gap-0.5">
          <h2 className="text-lg font-bold text-foreground flex items-center gap-2">
            <ListTodo className="w-5 h-5 text-primary" />
            Tasks
          </h2>
          <p className="text-xs text-muted-foreground">Manage your system tasks</p>
        </div>
        <Badge variant="outline" className="gap-1">
          <CheckCircle className="w-3 h-3" />
          {tasks.length} total
        </Badge>
      </div>

      {/* Add Form */}
      <form onSubmit={handleAdd} className="flex gap-3">
        <Input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="Enter a new task..."
          className="flex-1"
        />
        <Button type="submit" variant="gradient" className="shrink-0">
          <Plus className="w-4 h-4" />
          Add Task
        </Button>
      </form>

      {/* Table */}
      <Card>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-12">#</TableHead>
              <TableHead>Title</TableHead>
              <TableHead>Created</TableHead>
              <TableHead className="w-20 text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {tasks.map((t, idx) => (
              <TableRow key={t.id} className="group">
                <TableCell className="font-mono text-xs text-muted-foreground">{idx + 1}</TableCell>
                <TableCell>
                  <div className="flex items-center gap-2">
                    <div className="w-1.5 h-1.5 rounded-full bg-primary/60 shrink-0" />
                    <span className="font-medium text-foreground">{t.title}</span>
                  </div>
                </TableCell>
                <TableCell>
                  <div className="flex items-center gap-1.5 text-muted-foreground">
                    <Calendar className="w-3 h-3" />
                    <span className="text-xs">{new Date(t.createdAt).toLocaleString()}</span>
                  </div>
                </TableCell>
                <TableCell className="text-right">
                  <Button
                    variant="destructive"
                    size="icon"
                    className="h-7 w-7 rounded-lg opacity-0 group-hover:opacity-100 transition-opacity"
                    onClick={() => setDeleteId(t.id)}
                  >
                    <Trash2 className="w-3 h-3" />
                  </Button>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        {tasks.length === 0 && (
          <TableEmpty>
            <ListTodo className="w-10 h-10 mb-2 text-muted-foreground/30" />
            <p className="text-sm">No tasks yet — add one above!</p>
          </TableEmpty>
        )}
      </Card>

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!deleteId} onOpenChange={(open) => !open && setDeleteId(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Task</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete this task? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <DialogClose asChild>
              <Button variant="outline">Cancel</Button>
            </DialogClose>
            <Button variant="destructive" onClick={handleDelete}>
              <Trash2 className="w-4 h-4" />
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
