import { cn } from "@/lib/utils";

function Card({ className, ...props }) {
  return (
    <div
      className={cn(
        "rounded-2xl border border-border bg-card overflow-hidden transition-all duration-300",
        "hover:border-primary/20 hover:shadow-lg hover:shadow-primary/5",
        className
      )}
      {...props}
    />
  );
}

function CardHeader({ className, ...props }) {
  return (
    <div
      className={cn(
        "flex items-center justify-between px-5 py-4 border-b border-border/60",
        className
      )}
      {...props}
    />
  );
}

function CardTitle({ className, ...props }) {
  return (
    <h3
      className={cn("text-sm font-semibold text-foreground tracking-tight", className)}
      {...props}
    />
  );
}

function CardDescription({ className, ...props }) {
  return (
    <p className={cn("text-xs text-muted-foreground", className)} {...props} />
  );
}

function CardContent({ className, ...props }) {
  return (
    <div className={cn("px-5 py-4", className)} {...props} />
  );
}

function CardFooter({ className, ...props }) {
  return (
    <div className={cn("flex items-center px-5 py-3 border-t border-border/60", className)} {...props} />
  );
}

export { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter };
