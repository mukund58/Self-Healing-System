import { useEffect, useState } from "react";
import {
  Brain, TrendingUp, CheckCircle, XCircle, Target, Clock,
  BarChart3, Sparkles, ArrowRight, Award, Lightbulb, Activity,
} from "lucide-react";
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip as RechartsTooltip,
  ResponsiveContainer, Cell,
} from "recharts";
import { getLearningReport } from "@/api";
import { cn } from "@/lib/utils";
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from "@/components/ui/Card";
import { Badge } from "@/components/ui/Badge";
import { Progress } from "@/components/ui/Progress";
import { ScrollArea } from "@/components/ui/ScrollArea";
import { Separator } from "@/components/ui/Separator";

export function LearningTab() {
  const [report, setReport] = useState(null);

  useEffect(() => {
    const load = () => getLearningReport().then(setReport).catch(() => {});
    load();
    const i = setInterval(load, 5000);
    return () => clearInterval(i);
  }, []);

  const patterns = report?.patterns || [];
  const events = report?.recentEvents || [];

  // Group patterns by rootCause for the overview
  const rootCauses = {};
  patterns.forEach((p) => {
    if (!rootCauses[p.rootCause]) rootCauses[p.rootCause] = [];
    rootCauses[p.rootCause].push(p);
  });

  // Chart data: success rate per strategy
  const chartData = patterns.map((p) => ({
    name: `${abbreviate(p.rootCause)}→${abbreviate(p.strategyName)}`,
    rate: Math.round(p.successRate * 100),
    attempts: p.totalAttempts,
    rootCause: p.rootCause,
    strategy: p.strategyName,
  }));

  // Stats
  const totalAttempts = patterns.reduce((s, p) => s + p.totalAttempts, 0);
  const totalSuccesses = patterns.reduce((s, p) => s + p.successes, 0);
  const overallRate = totalAttempts > 0 ? Math.round((totalSuccesses / totalAttempts) * 100) : 0;
  const bestPattern = patterns.length > 0
    ? [...patterns].sort((a, b) => b.successRate - a.successRate || b.totalAttempts - a.totalAttempts)[0]
    : null;

  return (
    <div className="flex flex-col gap-6 animate-[fade-up_0.4s_ease-out]">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex flex-col gap-0.5">
          <h2 className="text-lg font-bold text-foreground flex items-center gap-2">
            <Brain className="w-5 h-5 text-accent" />
            Learning Loop
          </h2>
          <p className="text-xs text-muted-foreground">
            AI learns which remediation strategies work best for each failure type
          </p>
        </div>
        <div className="flex gap-2">
          <Badge variant="info" className="gap-1">
            <Sparkles className="w-3 h-3" />
            {patterns.length} patterns
          </Badge>
          <Badge variant="outline" className="gap-1">
            <Activity className="w-3 h-3" />
            {totalAttempts} attempts
          </Badge>
        </div>
      </div>

      {/* Stat Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <StatMini
          icon={<TrendingUp className="w-4 h-4" />}
          label="Overall Success Rate"
          value={`${overallRate}%`}
          valueClass={overallRate >= 80 ? "text-success" : overallRate >= 50 ? "text-warning" : "text-destructive"}
          accent="from-success to-primary"
        >
          <Progress
            value={overallRate}
            className="w-16 h-1.5"
            indicatorClass={overallRate >= 80 ? "bg-success" : overallRate >= 50 ? "bg-warning" : "bg-destructive"}
          />
        </StatMini>

        <StatMini
          icon={<Target className="w-4 h-4" />}
          label="Root Causes Tracked"
          value={Object.keys(rootCauses).length}
          accent="from-accent to-primary"
        />

        <StatMini
          icon={<Award className="w-4 h-4" />}
          label="Best Strategy"
          value={bestPattern ? abbreviate(bestPattern.strategyName) : "—"}
          valueClass="text-primary text-base"
          accent="from-primary to-accent"
        >
          {bestPattern && (
            <Badge variant="success" className="text-[0.55rem]">
              {Math.round(bestPattern.successRate * 100)}%
            </Badge>
          )}
        </StatMini>
      </div>

      {/* Chart + Root Cause Breakdown */}
      <div className="grid grid-cols-1 lg:grid-cols-5 gap-4">
        {/* Strategy Chart */}
        <Card className="lg:col-span-3">
          <CardHeader>
            <div className="flex flex-col gap-0.5">
              <CardTitle className="flex items-center gap-2">
                <BarChart3 className="w-4 h-4 text-primary" />
                Strategy Success Rates
              </CardTitle>
              <CardDescription>Success rate per root cause → strategy combination</CardDescription>
            </div>
          </CardHeader>
          <CardContent className="pb-2 px-2">
            {chartData.length > 0 ? (
              <ResponsiveContainer width="100%" height={260}>
                <BarChart data={chartData} margin={{ top: 5, right: 10, left: -10, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#1b2433" vertical={false} />
                  <XAxis
                    dataKey="name"
                    tick={{ fill: "#7d8590", fontSize: 10 }}
                    axisLine={false}
                    tickLine={false}
                    angle={-20}
                    textAnchor="end"
                    height={50}
                  />
                  <YAxis
                    tick={{ fill: "#7d8590", fontSize: 10 }}
                    axisLine={false}
                    tickLine={false}
                    domain={[0, 100]}
                    unit="%"
                  />
                  <RechartsTooltip
                    contentStyle={{
                      background: "#111820",
                      border: "1px solid #1b2433",
                      borderRadius: "12px",
                      fontSize: "12px",
                      color: "#e6edf3",
                    }}
                    formatter={(val, name, props) => [
                      `${val}% (${props.payload.attempts} attempts)`,
                      "Success Rate",
                    ]}
                    labelFormatter={(label) => label}
                  />
                  <Bar dataKey="rate" radius={[6, 6, 0, 0]} maxBarSize={48}>
                    {chartData.map((entry, idx) => (
                      <Cell
                        key={idx}
                        fill={entry.rate >= 80 ? "#34d399" : entry.rate >= 50 ? "#fbbf24" : "#f87171"}
                        fillOpacity={0.85}
                      />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <EmptyChart />
            )}
          </CardContent>
        </Card>

        {/* Root Cause Breakdown */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <div className="flex flex-col gap-0.5">
              <CardTitle className="flex items-center gap-2">
                <Lightbulb className="w-4 h-4 text-warning" />
                Root Cause Intelligence
              </CardTitle>
              <CardDescription>Best strategy per failure type</CardDescription>
            </div>
          </CardHeader>
          <CardContent>
            <ScrollArea className="max-h-[260px]">
              <div className="space-y-3">
                {Object.keys(rootCauses).length === 0 && (
                  <div className="flex flex-col items-center justify-center py-10 text-muted-foreground/40">
                    <Brain className="w-8 h-8 mb-2" />
                    <p className="text-sm">No patterns learned yet</p>
                    <p className="text-xs mt-1">Data appears after recovery actions</p>
                  </div>
                )}
                {Object.entries(rootCauses).map(([cause, strategies]) => {
                  const best = [...strategies].sort((a, b) => b.successRate - a.successRate)[0];
                  const totalAtt = strategies.reduce((s, p) => s + p.totalAttempts, 0);
                  return (
                    <div key={cause} className="flex flex-col gap-2 p-3 rounded-xl bg-secondary/30 border border-border/40">
                      <div className="flex items-center justify-between">
                        <Badge className="text-[0.65rem]">{cause}</Badge>
                        <span className="text-[0.65rem] text-muted-foreground">{totalAtt} attempts</span>
                      </div>
                      {strategies.map((s) => (
                        <div key={s.strategyName} className="flex items-center gap-2">
                          <ArrowRight className="w-3 h-3 text-muted-foreground shrink-0" />
                          <span className="text-[0.72rem] text-foreground flex-1 truncate">{s.strategyName}</span>
                          <div className="flex items-center gap-1.5">
                            <Progress
                              value={Math.round(s.successRate * 100)}
                              className="w-12 h-1"
                              indicatorClass={s.successRate >= 0.8 ? "bg-success" : s.successRate >= 0.5 ? "bg-warning" : "bg-destructive"}
                            />
                            <span className={cn(
                              "text-[0.65rem] font-semibold w-8 text-right",
                              s.successRate >= 0.8 ? "text-success" : s.successRate >= 0.5 ? "text-warning" : "text-destructive"
                            )}>
                              {Math.round(s.successRate * 100)}%
                            </span>
                            {s.strategyName === best.strategyName && (
                              <Award className="w-3 h-3 text-warning shrink-0" />
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  );
                })}
              </div>
            </ScrollArea>
          </CardContent>
        </Card>
      </div>

      {/* Recent Learning Events */}
      <Card>
        <CardHeader>
          <div className="flex flex-col gap-0.5">
            <CardTitle className="flex items-center gap-2">
              <Clock className="w-4 h-4 text-muted-foreground" />
              Recent Learning Events
            </CardTitle>
            <CardDescription>Every recovery outcome is recorded and used to improve decisions</CardDescription>
          </div>
          <Badge variant="outline">{events.length}</Badge>
        </CardHeader>
        <CardContent className="p-0">
          <ScrollArea className="max-h-80">
            <div className="px-4 py-2 space-y-0.5">
              {events.length === 0 && (
                <div className="flex flex-col items-center justify-center py-10 text-muted-foreground/40">
                  <Sparkles className="w-8 h-8 mb-2" />
                  <p className="text-sm">No events yet</p>
                </div>
              )}
              {events.map((evt, i) => (
                <div
                  key={i}
                  className="flex items-center gap-3 py-2.5 px-2 rounded-lg transition-all duration-200 hover:bg-secondary/40"
                >
                  <div className={cn(
                    "flex items-center justify-center w-7 h-7 rounded-lg shrink-0",
                    evt.success ? "bg-success/10" : "bg-destructive/10"
                  )}>
                    {evt.success
                      ? <CheckCircle className="w-3.5 h-3.5 text-success" />
                      : <XCircle className="w-3.5 h-3.5 text-destructive" />}
                  </div>
                  <div className="flex flex-col gap-0.5 flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <Badge className="text-[0.55rem] px-1.5 py-0 h-4">{evt.rootCause}</Badge>
                      <ArrowRight className="w-3 h-3 text-muted-foreground" />
                      <span className="text-[0.72rem] font-medium text-foreground">{evt.strategyName}</span>
                    </div>
                    <div className="flex items-center gap-3 text-[0.65rem] text-muted-foreground">
                      <span>Success rate: {Math.round(evt.newSuccessRate * 100)}%</span>
                      <span>Total: {evt.totalAttempts} attempts</span>
                    </div>
                  </div>
                  <span className="text-[0.6rem] text-muted-foreground/50 shrink-0">
                    {new Date(evt.timestamp).toLocaleTimeString()}
                  </span>
                </div>
              ))}
            </div>
          </ScrollArea>
        </CardContent>
      </Card>
    </div>
  );
}

function StatMini({ icon, label, value, valueClass, accent, children }) {
  return (
    <div className="group relative flex flex-col rounded-2xl border border-border bg-card p-4 overflow-hidden transition-all duration-300 hover:border-primary/20 hover:shadow-lg hover:shadow-primary/5">
      <div className={cn("absolute top-0 left-0 w-full h-[2px] opacity-60 group-hover:opacity-100 transition-opacity bg-gradient-to-r", accent)} />
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center justify-center w-8 h-8 rounded-xl bg-secondary/80 text-muted-foreground group-hover:text-primary transition-colors">
          {icon}
        </div>
        {children}
      </div>
      <span className="text-[0.65rem] font-medium uppercase text-muted-foreground tracking-wider mb-0.5">{label}</span>
      <span className={cn("text-xl font-bold leading-tight tracking-tight", valueClass || "text-foreground")}>{value}</span>
    </div>
  );
}

function EmptyChart() {
  return (
    <div className="flex items-center justify-center h-[260px] text-muted-foreground/40 text-sm">
      <div className="flex flex-col items-center gap-2">
        <BarChart3 className="w-8 h-8" />
        <span>No strategy data yet</span>
        <span className="text-xs text-muted-foreground/30">Patterns appear after recovery actions</span>
      </div>
    </div>
  );
}

function abbreviate(str) {
  // "RestartWithBuffer" → "RestartBuf", "MemoryLeakSuspected" → "MemLeak"
  const map = {
    RestartWithBuffer: "RstrtBuf",
    ScaleUpAggressive: "ScaleAggr",
    RestartAndMonitor: "RstrtMon",
    ScaleAndRestart: "ScaleRstrt",
    ScaleUpForCpu: "ScaleCPU",
    DefaultRestart: "DfltRstrt",
    RestartAndScale: "RstrtScale",
    MemoryLeakSuspected: "MemLeak",
    ResourceExhaustion: "ResExhaust",
    TrafficOverload: "TrafficOvld",
    ApplicationError: "AppError",
    DependencyFailure: "DepFail",
    HighCpuUsage: "HighCPU",
    HighErrorRate: "HighErr",
  };
  return map[str] || (str.length > 12 ? str.slice(0, 10) + "…" : str);
}
