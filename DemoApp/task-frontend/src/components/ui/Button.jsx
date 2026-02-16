import { Slot } from "@radix-ui/react-slot";
import { cva } from "class-variance-authority";
import { cn } from "@/lib/utils";

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-xl text-sm font-medium transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/40 focus-visible:ring-offset-2 focus-visible:ring-offset-background disabled:pointer-events-none disabled:opacity-40 cursor-pointer active:scale-[0.97]",
  {
    variants: {
      variant: {
        default: "bg-primary text-primary-foreground shadow-md shadow-primary/20 hover:shadow-lg hover:shadow-primary/30 hover:brightness-110",
        destructive: "bg-destructive/10 border border-destructive/30 text-destructive hover:bg-destructive/20 hover:border-destructive/50",
        outline: "border border-border bg-transparent hover:bg-secondary hover:border-primary/30 hover:text-foreground",
        secondary: "bg-secondary text-secondary-foreground hover:bg-secondary/80 hover:text-foreground",
        ghost: "hover:bg-secondary/80 hover:text-foreground",
        success: "bg-success/10 border border-success/30 text-success hover:bg-success/20 hover:border-success/50",
        gradient: "bg-gradient-to-r from-primary via-cyan-400 to-accent text-black font-semibold shadow-md shadow-primary/25 hover:shadow-lg hover:shadow-primary/40 hover:brightness-105",
      },
      size: {
        default: "h-9 px-4 py-2",
        sm: "h-8 px-3 text-xs rounded-lg",
        lg: "h-11 px-8 text-base",
        icon: "h-9 w-9",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
);

function Button({ className, variant, size, asChild = false, ...props }) {
  const Comp = asChild ? Slot : "button";
  return (
    <Comp className={cn(buttonVariants({ variant, size, className }))} {...props} />
  );
}

export { Button, buttonVariants };
