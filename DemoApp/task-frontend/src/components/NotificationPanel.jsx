import { useState } from "react";
import { X, AlertTriangle, CheckCircle, RefreshCw, Bell } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { ScrollArea } from "@/components/ui/ScrollArea";
import { Separator } from "@/components/ui/Separator";
import { cn, timeAgo, severityBadge } from "@/lib/utils";

export function NotificationPanel({ alerts, recoveries, onClose }) {
  const [panelTab, setPanelTab] = useState("alerts");
  const unresolvedAlerts = alerts.filter((a) => !a.resolved);
  const resolvedAlerts = alerts.filter((a) => a.resolved);

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3.5 border-b border-border/60">
        <div className="flex items-center gap-2">
          <Bell className="w-4 h-4 text-primary" />
          <h3 className="text-sm font-semibold text-foreground">Notifications</h3>
          {unresolvedAlerts.length > 0 && (
            <Badge variant="destructive" className="text-[0.55rem] px-1.5 py-0 h-4">
              {unresolvedAlerts.length} new
            </Badge>
          )}
        </div>
        <Button variant="ghost" size="icon" className="h-7 w-7 rounded-lg" onClick={onClose}>
          <X className="w-3.5 h-3.5" />
        </Button>
      </div>

      {/* Tab Switcher */}
      <div className="flex">
        {[
          { key: "alerts", label: "Alerts", count: unresolvedAlerts.length, icon: AlertTriangle },
          { key: "recoveries", label: "Recoveries", count: recoveries.length, icon: RefreshCw },
        ].map(({ key, label, count, icon: Icon }) => (
          <button
            key={key}
            className={cn(
              "flex-1 py-2.5 text-xs font-medium text-center transition-all duration-200 border-b-2 cursor-pointer flex items-center justify-center gap-1.5",
              panelTab === key
                ? "text-primary border-primary bg-primary/5"
                : "text-muted-foreground border-transparent hover:text-foreground hover:bg-secondary/30"
            )}
            onClick={() => setPanelTab(key)}
          >
            <Icon className="w-3 h-3" />
            {label}
            {count > 0 && (
              <span className="text-[0.6rem] bg-secondary px-1.5 py-px rounded-full">{count}</span>
            )}
          </button>
        ))}
      </div>

      {/* Body */}
      <ScrollArea className="flex-1 max-h-[400px]">
        <div className="p-2 space-y-1">
          {panelTab === "alerts" && (
            <>
              {unresolvedAlerts.length === 0 && resolvedAlerts.length === 0 && (
                <EmptyState icon={<AlertTriangle className="w-8 h-8 text-muted-foreground/30" />} text="No alerts yet" />
              )}
              {unresolvedAlerts.map((a, i) => (
                <AlertItem key={`u-${i}`} alert={a} resolved={false} />
              ))}
              {resolvedAlerts.length > 0 && (
                <>
                  <div className="flex items-center gap-2 px-2 pt-3 pb-1">
                    <span className="text-[0.65rem] font-semibold uppercase text-muted-foreground/60 tracking-wider">Resolved</span>
                    <Separator className="flex-1" />
                  </div>
                  {resolvedAlerts.map((a, i) => (
                    <AlertItem key={`r-${i}`} alert={a} resolved={true} />
                  ))}
                </>
              )}
            </>
          )}

          {panelTab === "recoveries" && (
            <>
              {recoveries.length === 0 && (
                <EmptyState icon={<RefreshCw className="w-8 h-8 text-muted-foreground/30" />} text="No recoveries" />
              )}
              {recoveries.map((r, i) => (
                <RecoveryItem key={i} recovery={r} />
              ))}
            </>
          )}
        </div>
      </ScrollArea>
    </div>
  );
}

function EmptyState({ icon, text }) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 py-10">
      {icon}
      <p className="text-sm text-muted-foreground/60">{text}</p>
    </div>
  );
}

function AlertItem({ alert, resolved }) {
  return (
    <div
      className={cn(
        "flex items-start gap-3 px-3 py-2.5 rounded-xl transition-all duration-200",
        resolved
          ? "opacity-40 hover:opacity-60"
          : "hover:bg-secondary/40 bg-secondary/20"
      )}
    >
      <div className={cn(
        "flex items-center justify-center w-7 h-7 rounded-lg shrink-0 mt-0.5",
        resolved ? "bg-success/10" : "bg-destructive/10"
      )}>
        {resolved
          ? <CheckCircle className="w-3.5 h-3.5 text-success" />
          : <AlertTriangle className="w-3.5 h-3.5 text-destructive" />
        }
      </div>
      <div className="flex flex-col gap-0.5 min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <span className="text-[0.8rem] font-semibold text-foreground truncate">{alert.failureType}</span>
          {!resolved && <Badge variant={severityBadge(alert.severity)} className="text-[0.55rem] px-1.5 py-0 h-4">{alert.severity}</Badge>}
        </div>
        <span className="text-[0.72rem] text-muted-foreground truncate">{alert.description}</span>
        <span className="text-[0.63rem] text-muted-foreground/50">{timeAgo(alert.detectedAt)}</span>
      </div>
    </div>
  );
}

function RecoveryItem({ recovery }) {
  const isSuccess = recovery.status === "Success";
  return (
    <div className="flex items-start gap-3 px-3 py-2.5 rounded-xl transition-all duration-200 hover:bg-secondary/40">
      <div className={cn(
        "flex items-center justify-center w-7 h-7 rounded-lg shrink-0 mt-0.5",
        isSuccess ? "bg-success/10" : "bg-destructive/10"
      )}>
        {isSuccess
          ? <CheckCircle className="w-3.5 h-3.5 text-success" />
          : <X className="w-3.5 h-3.5 text-destructive" />
        }
      </div>
      <div className="flex flex-col gap-0.5 min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <span className="text-[0.8rem] font-semibold text-foreground">
            {recovery.actionType}
          </span>
          <Badge variant={isSuccess ? "success" : "destructive"} className="text-[0.55rem] px-1.5 py-0 h-4">{recovery.status}</Badge>
        </div>
        <span className="text-[0.72rem] text-muted-foreground truncate">{recovery.details}</span>
        <span className="text-[0.63rem] text-muted-foreground/50">{timeAgo(recovery.performedAt)}</span>
      </div>
    </div>
  );
}
