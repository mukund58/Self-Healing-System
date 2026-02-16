import { useEffect, useState, useMemo } from "react";
import {
  Activity, Monitor, AlertTriangle, RefreshCw, CheckCircle, XCircle,
  TrendingUp, Cpu, Server, Clock,
} from "lucide-react";
import {
  AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip as RechartsTooltip, ResponsiveContainer,
} from "recharts";
import { getAnalyzerStatus, getFailureEvents, getRecoveryActions } from "@/api";
import { cn, timeAgo, severityColor, severityBadge } from "@/lib/utils";
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { Progress } from "@/components/ui/Progress";
import { Separator } from "@/components/ui/Separator";
import { ScrollArea } from "@/components/ui/ScrollArea";
import { StatCard } from "@/components/StatCard";
import { PulseDot, StatusDot } from "@/components/PulseDot";

const GRAFANA_URL = import.meta.env.VITE_GRAFANA_URL || "http://localhost:3000";

export function DashboardTab({ health }) {
  const [status, setStatus] = useState(null);
  const [failures, setFailures] = useState([]);
  const [recoveryActions, setRecoveryActions] = useState([]);
  const [memoryHistory, setMemoryHistory] = useState([]);

  useEffect(() => {
    const load = () => {
      getAnalyzerStatus().then(setStatus).catch(() => setStatus("Unavailable"));
      getFailureEvents(10).then(setFailures).catch(() => {});
      getRecoveryActions(10).then(setRecoveryActions).catch(() => {});
    };
    load();
    const i = setInterval(load, 5000);
    return () => clearInterval(i);
  }, []);

  // Track memory over time for the chart
  useEffect(() => {
    if (health?.memoryMb) {
      setMemoryHistory((prev) => {
        const now = new Date();
        const entry = { time: now.toLocaleTimeString("en-US", { hour12: false, hour: "2-digit", minute: "2-digit", second: "2-digit" }), mb: health.memoryMb };
        const next = [...prev, entry].slice(-30);
        return next;
      });
    }
  }, [health?.memoryMb]);

  const totalRecoveries = recoveryActions.length;
  const successCount = recoveryActions.filter((r) => r.status === "Success").length;
  const failedCount = recoveryActions.filter((r) => r.status === "Failed").length;
  const unresolvedFailures = failures.filter((f) => !f.resolved).length;
  const successRate = totalRecoveries > 0 ? Math.round((successCount / totalRecoveries) * 100) : 0;
  const latestAnalysis = typeof status === "object" && status !== null ? status : null;

  return (
    <div className="flex flex-col gap-6 animate-[fade-up_0.4s_ease-out]">
      {/* Stat Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <StatCard
          icon={<Activity className="w-5 h-5" />}
          label="System Health"
          value={health?.status || "—"}
          valueClass={health?.status === "OK" ? "text-success" : "text-destructive"}
          accentClass="bg-gradient-to-r from-primary to-success"
        >
          {health?.status === "OK"
            ? <Badge variant="success" className="text-[0.6rem]"><CheckCircle className="w-3 h-3 mr-1" />Online</Badge>
            : <Badge variant="destructive" className="text-[0.6rem]"><XCircle className="w-3 h-3 mr-1" />Offline</Badge>
          }
        </StatCard>

        <StatCard
          icon={<Cpu className="w-5 h-5" />}
          label="Memory Usage"
          value={<>{health?.memoryMb ?? "—"} <span className="text-sm font-medium text-muted-foreground">MB</span></>}
          accentClass="bg-gradient-to-r from-accent to-primary"
          sub={health?.threadCount ? `${health.threadCount} active threads` : undefined}
        />

        <StatCard
          icon={<AlertTriangle className="w-5 h-5" />}
          label="Active Alerts"
          value={unresolvedFailures}
          valueClass={unresolvedFailures > 0 ? "text-warning" : "text-success"}
          accentClass="bg-gradient-to-r from-warning to-destructive"
          sub={`${failures.length} total detected`}
        />

        <StatCard
          icon={<RefreshCw className="w-5 h-5" />}
          label="Recovery Rate"
          value={`${successRate}%`}
          valueClass={successRate >= 80 ? "text-success" : successRate >= 50 ? "text-warning" : "text-destructive"}
          accentClass="bg-gradient-to-r from-success to-primary"
          sub={`${successCount} ok · ${failedCount} failed`}
        >
          <Progress
            value={successRate}
            className="w-12 h-1.5"
            indicatorClass={successRate >= 80 ? "bg-success" : successRate >= 50 ? "bg-warning" : "bg-destructive"}
          />
        </StatCard>
      </div>

      {/* Memory Chart + Detection */}
      <div className="grid grid-cols-1 lg:grid-cols-5 gap-4">
        {/* Memory Chart */}
        <Card className="lg:col-span-3">
          <CardHeader>
            <div className="flex flex-col gap-0.5">
              <CardTitle className="flex items-center gap-2">
                <TrendingUp className="w-4 h-4 text-primary" />
                Memory Timeline
              </CardTitle>
              <CardDescription>Real-time memory usage over time</CardDescription>
            </div>
            <Badge variant="muted" className="text-[0.6rem]">
              <Clock className="w-3 h-3 mr-1" />
              Live
            </Badge>
          </CardHeader>
          <CardContent className="pb-2 px-2">
            {memoryHistory.length > 1 ? (
              <ResponsiveContainer width="100%" height={220}>
                <AreaChart data={memoryHistory} margin={{ top: 5, right: 10, left: -10, bottom: 0 }}>
                  <defs>
                    <linearGradient id="memGrad" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="0%" stopColor="#22d3ee" stopOpacity={0.3} />
                      <stop offset="95%" stopColor="#22d3ee" stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" stroke="#1b2433" vertical={false} />
                  <XAxis dataKey="time" tick={{ fill: "#7d8590", fontSize: 10 }} axisLine={false} tickLine={false} />
                  <YAxis tick={{ fill: "#7d8590", fontSize: 10 }} axisLine={false} tickLine={false} unit=" MB" />
                  <RechartsTooltip
                    contentStyle={{ background: "#111820", border: "1px solid #1b2433", borderRadius: "12px", fontSize: "12px", color: "#e6edf3" }}
                    labelStyle={{ color: "#7d8590" }}
                  />
                  <Area type="monotone" dataKey="mb" stroke="#22d3ee" strokeWidth={2} fill="url(#memGrad)" dot={false} />
                </AreaChart>
              </ResponsiveContainer>
            ) : (
              <div className="flex items-center justify-center h-[220px] text-muted-foreground/40 text-sm">
                <div className="flex flex-col items-center gap-2">
                  <Monitor className="w-8 h-8" />
                  <span>Collecting data...</span>
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Latest Detection */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <div className="flex flex-col gap-0.5">
              <CardTitle className="flex items-center gap-2">
                <Server className="w-4 h-4 text-accent" />
                Latest Detection
              </CardTitle>
              <CardDescription>Most recent anomaly scan</CardDescription>
            </div>
            <PulseDot color={latestAnalysis && !latestAnalysis.resolved ? "bg-warning" : "bg-success"} />
          </CardHeader>
          <CardContent>
            {latestAnalysis ? (
              <div className="flex flex-col gap-3">
                <InfoRow label="Type">
                  <Badge>{latestAnalysis.failureType}</Badge>
                </InfoRow>
                <InfoRow label="Severity">
                  <Badge variant={severityBadge(latestAnalysis.severity)}>
                    {latestAnalysis.severity}
                  </Badge>
                </InfoRow>
                <Separator />
                <InfoRow label="Description">
                  <span className="text-sm text-card-foreground leading-relaxed">{latestAnalysis.description}</span>
                </InfoRow>
                <InfoRow label="Detected">
                  <span className="text-sm text-muted-foreground">{new Date(latestAnalysis.detectedAt).toLocaleString()}</span>
                </InfoRow>
                <InfoRow label="Resolved">
                  {latestAnalysis.resolved
                    ? <Badge variant="success"><CheckCircle className="w-3 h-3 mr-1" />Yes</Badge>
                    : <Badge variant="destructive"><XCircle className="w-3 h-3 mr-1" />No</Badge>
                  }
                </InfoRow>
              </div>
            ) : (
              <div className="flex flex-col items-center justify-center py-10 text-muted-foreground/40">
                <Server className="w-10 h-10 mb-2" />
                <p className="text-sm">No detection data</p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Recent Activity */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Recent Failures */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <AlertTriangle className="w-4 h-4 text-destructive" />
              Recent Failures
            </CardTitle>
            <Badge variant="outline">{failures.length}</Badge>
          </CardHeader>
          <CardContent className="p-0">
            <ScrollArea className="max-h-72">
              <div className="px-4 py-2 space-y-0.5">
                {failures.length === 0 && (
                  <div className="flex flex-col items-center justify-center py-10 text-muted-foreground/40">
                    <CheckCircle className="w-8 h-8 mb-2" />
                    <p className="text-sm">No failures detected</p>
                  </div>
                )}
                {failures.slice(0, 8).map((f) => (
                  <div key={f.id} className={cn(
                    "flex items-center gap-3 py-2.5 px-2 rounded-lg transition-all duration-200",
                    f.resolved ? "opacity-40" : "hover:bg-secondary/40"
                  )}>
                    <StatusDot active={!f.resolved} />
                    <div className="flex flex-col gap-0.5 flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="text-[0.8rem] font-semibold text-foreground">{f.failureType}</span>
                        {!f.resolved && <Badge variant={severityBadge(f.severity)} className="text-[0.55rem] px-1.5 py-0 h-4">{f.severity}</Badge>}
                      </div>
                      <span className="text-[0.72rem] text-muted-foreground truncate">{f.description}</span>
                    </div>
                    <span className="text-[0.65rem] text-muted-foreground/50 shrink-0">{timeAgo(f.detectedAt)}</span>
                  </div>
                ))}
              </div>
            </ScrollArea>
          </CardContent>
        </Card>

        {/* Recent Recoveries */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <RefreshCw className="w-4 h-4 text-success" />
              Recent Recoveries
            </CardTitle>
            <Badge variant="outline">{recoveryActions.length}</Badge>
          </CardHeader>
          <CardContent className="p-0">
            <ScrollArea className="max-h-72">
              <div className="px-4 py-2 space-y-0.5">
                {recoveryActions.length === 0 && (
                  <div className="flex flex-col items-center justify-center py-10 text-muted-foreground/40">
                    <RefreshCw className="w-8 h-8 mb-2" />
                    <p className="text-sm">No recovery actions</p>
                  </div>
                )}
                {recoveryActions.slice(0, 8).map((r) => (
                  <div key={r.id} className="flex items-center gap-3 py-2.5 px-2 rounded-lg transition-all duration-200 hover:bg-secondary/40">
                    <StatusDot active={r.status !== "Success"} />
                    <div className="flex flex-col gap-0.5 flex-1 min-w-0">
                      <span className="text-[0.8rem] font-semibold text-foreground">
                        {r.actionType}
                        <span className="text-muted-foreground font-normal"> → </span>
                        {r.targetDeployment}
                      </span>
                      <span className="text-[0.72rem] text-muted-foreground truncate">{r.details}</span>
                    </div>
                    <Badge variant={r.status === "Success" ? "success" : "destructive"} className="text-[0.55rem] shrink-0">
                      {r.status}
                    </Badge>
                  </div>
                ))}
              </div>
            </ScrollArea>
          </CardContent>
        </Card>
      </div>

      {/* Grafana */}
      <Card>
        <CardHeader>
          <div className="flex flex-col gap-0.5">
            <CardTitle className="flex items-center gap-2">
              <Monitor className="w-4 h-4 text-primary" />
              Grafana Metrics
            </CardTitle>
            <CardDescription>Prometheus metrics visualization</CardDescription>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          <iframe
            src={`${GRAFANA_URL}/d/self-healing-overview/self-healing-system?orgId=1&refresh=5s&kiosk`}
            className="w-full h-[600px] border-none rounded-b-2xl bg-background"
            title="Grafana Dashboard"
          />
        </CardContent>
      </Card>
    </div>
  );
}

function InfoRow({ label, children }) {
  return (
    <div className="flex items-start gap-4">
      <span className="min-w-[80px] text-[0.7rem] font-semibold uppercase text-muted-foreground tracking-wider pt-0.5">
        {label}
      </span>
      <div className="flex-1">{children}</div>
    </div>
  );
}
