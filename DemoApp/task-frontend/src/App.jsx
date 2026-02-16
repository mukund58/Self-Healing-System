import { useEffect, useState, useCallback } from "react";
import { getAnalyzerEvents, getAnalyzerRecoveries, getHealth } from "@/api";
import { TooltipProvider } from "@/components/ui/Tooltip";
import { Header } from "@/components/Header";
import { DashboardTab } from "@/components/DashboardTab";
import { TasksTab } from "@/components/TasksTab";
import { FailuresTab } from "@/components/FailuresTab";
import { RecoveriesTab } from "@/components/RecoveriesTab";
import { MetricsTab } from "@/components/MetricsTab";
import { LearningTab } from "@/components/LearningTab";
import "./index.css";

export default function App() {
  const [tab, setTab] = useState("Dashboard");
  const [alerts, setAlerts] = useState([]);
  const [recoveries, setRecoveries] = useState([]);
  const [health, setHealth] = useState(null);

  const loadNotifs = useCallback(() => {
    getAnalyzerEvents().then(setAlerts).catch(() => {});
    getAnalyzerRecoveries().then(setRecoveries).catch(() => {});
    getHealth().then(setHealth).catch(() => setHealth(null));
  }, []);

  useEffect(() => {
    loadNotifs();
    const i = setInterval(loadNotifs, 5000);
    return () => clearInterval(i);
  }, [loadNotifs]);

  return (
    <TooltipProvider delayDuration={200}>
      <div className="min-h-screen flex flex-col bg-background bg-mesh">
        <div className="max-w-[1400px] w-full mx-auto px-6">
          <Header
            tab={tab}
            onTabChange={setTab}
            health={health}
            alerts={alerts}
            recoveries={recoveries}
          />

          <main className="flex-1 py-6">
            {tab === "Dashboard" && <DashboardTab health={health} />}
            {tab === "Tasks" && <TasksTab />}
            {tab === "Failures" && <FailuresTab />}
            {tab === "Recoveries" && <RecoveriesTab />}
            {tab === "Metrics" && <MetricsTab />}
            {tab === "Learning" && <LearningTab />}
          </main>

          <footer className="flex items-center justify-center gap-3 py-5 mt-6 border-t border-border/40">
            <span className="text-xs text-muted-foreground/50">Self-Healing System &copy; 2026</span>
            <span className="w-1 h-1 rounded-full bg-border" />
            <span className="text-xs text-muted-foreground/50">Kubernetes Auto-Recovery Platform</span>
          </footer>
        </div>
      </div>
    </TooltipProvider>
  );
}
