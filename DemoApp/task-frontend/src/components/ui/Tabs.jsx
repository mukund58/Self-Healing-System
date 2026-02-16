import * as TabsPrimitive from "@radix-ui/react-tabs";
import { cn } from "@/lib/utils";

const Tabs = TabsPrimitive.Root;

function TabsList({ className, ...props }) {
  return (
    <TabsPrimitive.List
      className={cn(
        "inline-flex items-center gap-1 rounded-xl bg-secondary/50 border border-border p-1",
        className
      )}
      {...props}
    />
  );
}

function TabsTrigger({ className, ...props }) {
  return (
    <TabsPrimitive.Trigger
      className={cn(
        "inline-flex items-center justify-center whitespace-nowrap rounded-lg px-3.5 py-1.5",
        "text-[0.8rem] font-medium text-muted-foreground transition-all duration-200 cursor-pointer",
        "hover:text-foreground hover:bg-secondary",
        "data-[state=active]:text-primary data-[state=active]:bg-card data-[state=active]:font-semibold data-[state=active]:shadow-md data-[state=active]:shadow-primary/10 data-[state=active]:border data-[state=active]:border-primary/20",
        className
      )}
      {...props}
    />
  );
}

function TabsContent({ className, ...props }) {
  return (
    <TabsPrimitive.Content
      className={cn("mt-4 focus-visible:outline-none animate-[fade-up_0.3s_ease-out]", className)}
      {...props}
    />
  );
}

export { Tabs, TabsList, TabsTrigger, TabsContent };
