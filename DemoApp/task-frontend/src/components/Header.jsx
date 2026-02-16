import { useState } from "react";
import { Shield, Bell, Zap } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/Tabs";
import { Popover, PopoverTrigger, PopoverContent } from "@/components/ui/Popover";
import { Separator } from "@/components/ui/Separator";
import { PulseDot } from "@/components/PulseDot";
import { NotificationPanel } from "@/components/NotificationPanel";

const NAV_TABS = ["Dashboard", "Tasks", "Failures", "Recoveries", "Metrics", "Learning"];

export function Header({ tab, onTabChange, health, alerts, recoveries }) {
  const [notifOpen, setNotifOpen] = useState(false);
  const unresolvedCount = alerts.filter((a) => !a.resolved).length;
  const isHealthy = health?.status === "OK";

  return (
    <header className="sticky top-0 z-50 glass border-b border-border/60">
      <div className="flex items-center justify-between py-3">
        {/* Brand */}
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2.5">
            <div className="flex items-center justify-center w-8 h-8 rounded-xl bg-gradient-to-br from-primary/20 to-accent/20 border border-primary/20">
              <Shield className="w-4.5 h-4.5 text-primary" />
            </div>
            <div className="flex flex-col">
              <h1 className="text-sm font-bold bg-gradient-to-r from-primary via-cyan-300 to-accent bg-clip-text text-transparent tracking-tight leading-tight">
                Self-Healing System
              </h1>
              <span className="text-[0.6rem] text-muted-foreground tracking-wide">KUBERNETES MONITORING</span>
            </div>
          </div>

          <Separator orientation="vertical" className="h-8 bg-border/40" />

          {/* Status Pill */}
          <div className="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-secondary/50 border border-border/60">
            <PulseDot color={isHealthy ? "bg-success" : "bg-destructive"} size="sm" />
            <div className="flex items-center gap-1.5">
              <Badge variant={isHealthy ? "success" : "destructive"} className="text-[0.6rem] px-1.5 py-0">
                {isHealthy ? "Healthy" : "Degraded"}
              </Badge>
              {health && (
                <span className="text-[0.65rem] text-muted-foreground hidden sm:inline">
                  {health.memoryMb} MB · {health.threadCount} thr
                </span>
              )}
            </div>
          </div>
        </div>

        {/* Center: Navigation */}
        <div className="absolute left-1/2 -translate-x-1/2 hidden lg:block">
          <Tabs value={tab} onValueChange={onTabChange}>
            <TabsList>
              {NAV_TABS.map((t) => (
                <TabsTrigger key={t} value={t}>{t}</TabsTrigger>
              ))}
            </TabsList>
          </Tabs>
        </div>

        {/* Right */}
        <div className="flex items-center gap-2">
          {/* Mobile Tabs */}
          <div className="lg:hidden">
            <Tabs value={tab} onValueChange={onTabChange}>
              <TabsList>
                {NAV_TABS.map((t) => (
                  <TabsTrigger key={t} value={t} className="text-[0.7rem] px-2.5">{t}</TabsTrigger>
                ))}
              </TabsList>
            </Tabs>
          </div>

          {/* Notification Bell */}
          <Popover open={notifOpen} onOpenChange={setNotifOpen}>
            <PopoverTrigger asChild>
              <Button variant="ghost" size="icon" className="relative h-9 w-9 rounded-xl">
                <Bell className="w-[18px] h-[18px] text-muted-foreground" />
                {unresolvedCount > 0 && (
                  <span className="absolute -top-0.5 -right-0.5 flex items-center justify-center bg-destructive text-white text-[0.55rem] font-bold min-w-[18px] h-[18px] px-1 rounded-full shadow-lg shadow-destructive/40 animate-[pulse-ring_2s_ease-in-out_infinite]">
                    {unresolvedCount > 99 ? "99+" : unresolvedCount}
                  </span>
                )}
              </Button>
            </PopoverTrigger>
            <PopoverContent>
              <NotificationPanel
                alerts={alerts}
                recoveries={recoveries}
                onClose={() => setNotifOpen(false)}
              />
            </PopoverContent>
          </Popover>
        </div>
      </div>
    </header>
  );
}
