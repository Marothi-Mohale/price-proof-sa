import type { ButtonHTMLAttributes } from "react";
import { cn } from "@/lib/utils";

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: "primary" | "secondary" | "ghost" | "danger";
  busy?: boolean;
};

export function buttonClasses(variant: NonNullable<ButtonProps["variant"]> = "primary", className?: string) {
  return cn(
    "inline-flex min-h-11 items-center justify-center rounded-2xl px-4 py-3 text-sm font-semibold transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-amber-500/50 disabled:cursor-not-allowed disabled:opacity-60",
    variant === "primary" && "bg-ink text-white shadow-lg shadow-ink/15 hover:bg-slate-900",
    variant === "secondary" && "border border-slate-300 bg-white text-slate-900 hover:border-slate-400 hover:bg-slate-50",
    variant === "ghost" && "bg-transparent text-slate-700 hover:bg-white/80",
    variant === "danger" && "bg-rose-600 text-white shadow-lg shadow-rose-600/20 hover:bg-rose-700",
    className
  );
}

export function Button({ className, children, variant = "primary", busy = false, disabled, ...props }: ButtonProps) {
  return (
    <button
      className={buttonClasses(variant, className)}
      disabled={disabled || busy}
      {...props}
    >
      {busy ? "Working..." : children}
    </button>
  );
}
