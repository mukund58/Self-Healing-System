import { cva } from "class-variance-authority";
import { cn } from "@/lib/utils";

const badgeVariants = cva(
  "inline-flex items-center rounded-lg px-2.5 py-0.5 text-[0.7rem] font-semibold whitespace-nowrap tracking-wide transition-colors",
  {
    variants: {
      variant: {
        default: "bg-primary/10 text-primary border border-primary/20",
        success: "bg-success/10 text-success border border-success/20",
        destructive: "bg-destructive/10 text-destructive border border-destructive/20",
        warning: "bg-warning/10 text-warning border border-warning/20",
        outline: "border border-border text-muted-foreground",
        muted: "bg-secondary text-muted-foreground",
        info: "bg-accent/10 text-accent border border-accent/20",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
);

function Badge({ className, variant, ...props }) {
  return (
    <span className={cn(badgeVariants({ variant, className }))} {...props} />
  );
}

export { Badge, badgeVariants };
