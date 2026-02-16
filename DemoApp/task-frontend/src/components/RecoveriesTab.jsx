import { useEffect, useState } from "react";
import { RefreshCw, CheckCircle, XCircle, Clock, Target } from "lucide-react";
import { getRecoveryActions } from "@/api";
import { cn } from "@/lib/utils";
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { Progress } from "@/components/ui/Progress";
import {
  Table, TableHeader, TableBody, TableRow, TableHead, TableCell, TableEmpty,
} from "@/components/ui/Table";

export function RecoveriesTab() {
  const [actions, setActions] = useState([]);

  const load = () => getRecoveryActions().then(setActions).catch(() => {});
  useEffect(() => {
    load();
    const i = setInterval(load, 5000);
    return () => clearInterval(i);
  }, []);

  const successCount = actions.filter((a) => a.status === "Success").length;
  const failedCount = actions.filter((a) => a.status === "Failed").length;
  const rate = actions.length > 0 ? Math.round((successCount / actions.length) * 100) : 0;

  return (
    <div className="flex flex-col gap-5 animate-[fade-up_0.4s_ease-out]">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex flex-col gap-0.5">
          <h2 className="text-lg font-bold text-foreground flex items-center gap-2">
            <RefreshCw className="w-5 h-5 text-success" />
            Recovery Actions
          </h2>
          <p className="text-xs text-muted-foreground">Automated healing actions performed by the system</p>
        </div>
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-secondary/50 border border-border/60">
            <span className="text-[0.7rem] text-muted-foreground">Success Rate</span>
            <Progress
              value={rate}
              className="w-16 h-1.5"
              indicatorClass={rate >= 80 ? "bg-success" : rate >= 50 ? "bg-warning" : "bg-destructive"}
            />
            <span className={cn("text-xs font-bold", rate >= 80 ? "text-success" : rate >= 50 ? "text-warning" : "text-destructive")}>{rate}%</span>
          </div>
          <div className="flex gap-2">
            <Badge variant="success" className="gap-1"><CheckCircle className="w-3 h-3" />{successCount}</Badge>
            <Badge variant="destructive" className="gap-1"><XCircle className="w-3 h-3" />{failedCount}</Badge>
            <Badge variant="outline" className="gap-1">{actions.length} total</Badge>
          </div>
        </div>
      </div>

      {/* Table */}
      <Card>
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Action</TableHead>
              <TableHead>Target</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Details</TableHead>
              <TableHead>Performed</TableHead>
              <TableHead>Completed</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {actions.map((a) => (
              <TableRow key={a.id}>
                <TableCell>
                  <Badge>{a.actionType}</Badge>
                </TableCell>
                <TableCell>
                  <div className="flex items-center gap-1.5">
                    <Target className="w-3 h-3 text-muted-foreground" />
                    <span className="font-mono text-xs text-foreground">{a.targetDeployment}</span>
                  </div>
                </TableCell>
                <TableCell>
                  <Badge variant={a.status === "Success" ? "success" : "destructive"} className="gap-1">
                    {a.status === "Success" ? <CheckCircle className="w-3 h-3" /> : <XCircle className="w-3 h-3" />}
                    {a.status}
                  </Badge>
                </TableCell>
                <TableCell>
                  <span className="max-w-[300px] truncate block text-xs text-muted-foreground">{a.details}</span>
                </TableCell>
                <TableCell>
                  <div className="flex items-center gap-1.5 text-muted-foreground">
                    <Clock className="w-3 h-3" />
                    <span className="text-xs">{new Date(a.performedAt).toLocaleString()}</span>
                  </div>
                </TableCell>
                <TableCell>
                  <span className="text-xs text-muted-foreground">
                    {a.completedAt ? new Date(a.completedAt).toLocaleString() : "—"}
                  </span>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
        {actions.length === 0 && (
          <TableEmpty>
            <RefreshCw className="w-10 h-10 mb-2 text-muted-foreground/30" />
            <p className="text-sm">No recovery actions</p>
            <p className="text-xs text-muted-foreground/40 mt-1">Actions will appear when anomalies are detected</p>
          </TableEmpty>
        )}
      </Card>
    </div>
  );
}
