import { cn } from "@/lib/utils";

export function PulseDot({ color = "bg-success", className, size = "default" }) {
  return (
    <span className={cn("relative inline-flex", className)}>
      <span
        className={cn(
          "animate-[pulse-ring_2s_ease-in-out_infinite] rounded-full absolute inset-0 opacity-40",
          color,
        )}
      />
      <span
        className={cn(
          "relative inline-block rounded-full",
          size === "sm" ? "w-1.5 h-1.5" : "w-2 h-2",
          color,
        )}
      />
    </span>
  );
}

export function StatusDot({ active, className }) {
  return (
    <span
      className={cn(
        "inline-block w-2 h-2 rounded-full shrink-0 ring-2",
        active
          ? "bg-destructive ring-destructive/20"
          : "bg-success ring-success/20",
        className
      )}
    />
  );
}
