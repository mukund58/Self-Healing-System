import { cn } from "@/lib/utils";

export function StatCard({ icon, label, value, valueClass, sub, accentClass, trend, children }) {
  return (
    <div className="group relative flex flex-col rounded-2xl border border-border bg-card p-5 overflow-hidden transition-all duration-300 hover:border-primary/20 hover:shadow-lg hover:shadow-primary/5">
      {/* Top accent bar */}
      <div className={cn("absolute top-0 left-0 w-full h-[2px] opacity-60 group-hover:opacity-100 transition-opacity", accentClass)} />

      <div className="flex items-start justify-between mb-3">
        {/* Icon */}
        <div className="flex items-center justify-center w-10 h-10 rounded-xl bg-secondary/80 text-muted-foreground group-hover:text-primary transition-colors">
          {icon}
        </div>

        {/* Trend or trailing content */}
        {children || (trend && (
          <span className={cn("text-xs font-medium px-2 py-0.5 rounded-lg", trend > 0 ? "text-success bg-success/10" : "text-destructive bg-destructive/10")}>
            {trend > 0 ? "+" : ""}{trend}%
          </span>
        ))}
      </div>

      {/* Label */}
      <span className="text-[0.7rem] font-medium uppercase text-muted-foreground tracking-wider mb-1">
        {label}
      </span>

      {/* Value */}
      <span className={cn("text-2xl font-bold leading-tight tracking-tight", valueClass || "text-foreground")}>
        {value}
      </span>

      {/* Sub text */}
      {sub && (
        <span className="text-[0.7rem] text-muted-foreground mt-1">
          {sub}
        </span>
      )}
    </div>
  );
}
