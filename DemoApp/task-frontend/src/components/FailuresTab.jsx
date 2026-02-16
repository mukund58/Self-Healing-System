import { useEffect, useState } from "react";
import { AlertTriangle, CheckCircle, Shield, Filter } from "lucide-react";
import { getFailureEvents, resolveFailure } from "@/api";
import { cn, severityColor, severityBadge } from "@/lib/utils";
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import {
  Table, TableHeader, TableBody, TableRow, TableHead, TableCell, TableEmpty,
} from "@/components/ui/Table";
import { Separator } from "@/components/ui/Separator";

const FILTERS = [
  { key: "all", label: "All Events" },
  { key: "active", label: "Active" },
  { key: "resolved", label: "Resolved" },
];

export function FailuresTab() {
  const [events, setEvents] = useState([]);
  const [filter, setFilter] = useState("all");

  const load = () => getFailureEvents().then(setEvents).catch(() => {});
  useEffect(() => {
    load();
    const i = setInterval(load, 5000);
    return () => clearInterval(i);
  }, []);

  const filtered =
    filter === "all" ? events
    : filter === "active" ? events.filter((e) => !e.resolved)
    : events.filter((e) => e.resolved);

  const activeCount = events.filter((e) => !e.resolved).length;
  const resolvedCount = events.filter((e) => e.resolved).length;

  return (
    <div className="flex flex-col gap-5 animate-[fade-up_0.4s_ease-out]">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex flex-col gap-0.5">
          <h2 className="text-lg font-bold text-foreground flex items-center gap-2">
            <AlertTriangle className="w-5 h-5 text-destructive" />
            Failure Events
          </h2>
          <p className="text-xs text-muted-foreground">Detected anomalies and system failures</p>
        </div>
        <div className="flex gap-2">
          {activeCount > 0 && <Badge variant="destructive">{activeCount} active</Badge>}
          <Badge variant="success">{resolvedCount} resolved</Badge>
          <Badge variant="outline">{events.length} total</Badge>
        </div>
      </div>

      {/* Filter Bar */}
      <div className="flex items-center gap-2">
        <Filter className="w-3.5 h-3.5 text-muted-foreground" />
        <div className="flex gap-1.5">
          {FILTERS.map((f) => (
            <button
              key={f.key}
              className={cn(
                "px-3.5 py-1.5 rounded-xl text-xs font-medium transition-all duration-200 cursor-pointer flex items-center gap-1.5",
                filter === f.key
                  ? "bg-primary/10 border border-primary/30 text-primary shadow-sm shadow-primary/10"
                  : "bg-secondary/50 border border-border text-muted-foreground hover:border-primary/20 hover:text-foreground"
              )}
              onClick={() => setFilter(f.key)}
            >
              {f.label}
              {f.key === "active" && activeCount > 0 && (
                <span className="bg-destructive/20 text-destructive text-[0.6rem] px-1.5 py-px rounded-full">{activeCount}</span>
              )}
            </button>
          ))}
        </div>
      </div>

      {/* Table */}
      <Card>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Type</TableHead>
              <TableHead>Severity</TableHead>
              <TableHead>Description</TableHead>
              <TableHead>Detected</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="w-24 text-right">Action</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filtered.map((e) => (
              <TableRow
                key={e.id}
                className={cn(e.resolved && "opacity-50")}
              >
                <TableCell>
                  <Badge>{e.failureType}</Badge>
                </TableCell>
                <TableCell>
                  <Badge variant={severityBadge(e.severity)}>
                    {e.severity}
                  </Badge>
                </TableCell>
                <TableCell>
                  <span className="max-w-[300px] truncate block text-sm">{e.description}</span>
                </TableCell>
                <TableCell>
                  <span className="text-xs text-muted-foreground">{new Date(e.detectedAt).toLocaleString()}</span>
                </TableCell>
                <TableCell>
                  {e.resolved ? (
                    <Badge variant="success"><CheckCircle className="w-3 h-3 mr-1" />Resolved</Badge>
                  ) : (
                    <Badge variant="destructive"><AlertTriangle className="w-3 h-3 mr-1" />Active</Badge>
                  )}
                </TableCell>
                <TableCell className="text-right">
                  {!e.resolved && (
                    <Button
                      variant="success"
                      size="sm"
                      onClick={() => resolveFailure(e.id).then(load)}
                    >
                      <Shield className="w-3 h-3" />
                      Resolve
                    </Button>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        {filtered.length === 0 && (
          <TableEmpty>
            <CheckCircle className="w-10 h-10 mb-2 text-muted-foreground/30" />
            <p className="text-sm">No failure events</p>
            <p className="text-xs text-muted-foreground/40 mt-1">System is operating normally</p>
          </TableEmpty>
        )}
      </Card>
    </div>
  );
}
