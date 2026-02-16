import * as ProgressPrimitive from "@radix-ui/react-progress";
import { cn } from "@/lib/utils";

function Progress({ className, value, indicatorClass, ...props }) {
  return (
    <ProgressPrimitive.Root
      className={cn(
        "relative h-2 w-full overflow-hidden rounded-full bg-secondary",
        className
      )}
      {...props}
    >
      <ProgressPrimitive.Indicator
        className={cn(
          "h-full rounded-full transition-all duration-500 ease-out",
          indicatorClass || "bg-primary"
        )}
        style={{ width: `${value || 0}%` }}
      />
    </ProgressPrimitive.Root>
  );
}

export { Progress };
