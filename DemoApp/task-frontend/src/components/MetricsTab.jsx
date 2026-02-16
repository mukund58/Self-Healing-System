import { useEffect, useState } from "react";
import { BarChart3, Clock, Hash, Activity } from "lucide-react";
import { getMetrics } from "@/api";
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import {
  Table, TableHeader, TableBody, TableRow, TableHead, TableCell, TableEmpty,
} from "@/components/ui/Table";

export function MetricsTab() {
  const [metrics, setMetrics] = useState([]);

  const load = () => getMetrics(100).then(setMetrics).catch(() => {});
  useEffect(() => {
    load();
    const i = setInterval(load, 5000);
    return () => clearInterval(i);
  }, []);

  // Group metrics by type for stats
  const types = {};
  metrics.forEach((m) => {
    types[m.metricType] = (types[m.metricType] || 0) + 1;
  });

  return (
    <div className="flex flex-col gap-5 animate-[fade-up_0.4s_ease-out]">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex flex-col gap-0.5">
          <h2 className="text-lg font-bold text-foreground flex items-center gap-2">
            <BarChart3 className="w-5 h-5 text-accent" />
            Metric Records
          </h2>
          <p className="text-xs text-muted-foreground">Raw metrics collected from Prometheus</p>
        </div>
        <div className="flex gap-2">
          {Object.entries(types).map(([type, count]) => (
            <Badge key={type} variant="info" className="gap-1">
              <Activity className="w-3 h-3" />
              {type}: {count}
            </Badge>
          ))}
          <Badge variant="outline">{metrics.length} records</Badge>
        </div>
      </div>

      {/* Table */}
      <Card>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Metric ID</TableHead>
              <TableHead>Type</TableHead>
              <TableHead>Value</TableHead>
              <TableHead>Recorded</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {metrics.map((m) => (
              <TableRow key={m.id}>
                <TableCell>
                  <div className="flex items-center gap-1.5">
                    <Hash className="w-3 h-3 text-muted-foreground" />
                    <span className="font-mono text-xs text-foreground">{m.metricId}</span>
                  </div>
                </TableCell>
                <TableCell>
                  <Badge variant="info">{m.metricType}</Badge>
                </TableCell>
                <TableCell>
                  <span className="font-mono text-sm font-semibold text-primary">{m.metricValue?.toFixed(2)}</span>
                </TableCell>
                <TableCell>
                  <div className="flex items-center gap-1.5 text-muted-foreground">
                    <Clock className="w-3 h-3" />
                    <span className="text-xs">{new Date(m.recordedAt).toLocaleString()}</span>
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        {metrics.length === 0 && (
          <TableEmpty>
            <BarChart3 className="w-10 h-10 mb-2 text-muted-foreground/30" />
            <p className="text-sm">No metrics stored yet</p>
            <p className="text-xs text-muted-foreground/40 mt-1">Metrics are collected when the analyzer runs</p>
          </TableEmpty>
        )}
      </Card>
    </div>
  );
}
