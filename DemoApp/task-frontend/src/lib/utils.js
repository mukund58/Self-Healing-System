import { clsx } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs) {
  return twMerge(clsx(inputs));
}

export function timeAgo(date) {
  const s = Math.floor((Date.now() - new Date(date).getTime()) / 1000);
  if (s < 10) return "just now";
  if (s < 60) return `${s}s ago`;
  const m = Math.floor(s / 60);
  if (m < 60) return `${m}m ago`;
  const h = Math.floor(m / 60);
  if (h < 24) return `${h}h ago`;
  return `${Math.floor(h / 24)}d ago`;
}

export function formatNumber(n) {
  if (typeof n !== "number") return "—";
  return n.toLocaleString("en-US");
}

export function severityColor(severity) {
  const s = severity?.toLowerCase();
  if (s === "critical") return "text-destructive";
  if (s === "warning") return "text-warning";
  return "text-primary";
}

export function severityBadge(severity) {
  const s = severity?.toLowerCase();
  if (s === "critical") return "destructive";
  if (s === "warning") return "warning";
  return "default";
}
