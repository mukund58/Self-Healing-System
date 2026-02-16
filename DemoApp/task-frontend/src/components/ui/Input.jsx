import { cn } from "@/lib/utils";

function Input({ className, ...props }) {
  return (
    <input
      className={cn(
        "flex h-10 w-full rounded-xl border border-border bg-secondary/50 px-4 py-2 text-sm text-foreground",
        "placeholder:text-muted-foreground/50 outline-none",
        "focus:border-primary/50 focus:ring-2 focus:ring-primary/15 focus:bg-card",
        "transition-all duration-200",
        className
      )}
      {...props}
    />
  );
}

export { Input };
