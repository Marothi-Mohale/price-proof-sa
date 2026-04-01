import { cn } from "@/lib/utils";

export function LogoMark({ className, compact = false }: { className?: string; compact?: boolean }) {
  return (
    <div className={cn("inline-flex items-center gap-3", className)}>
      <div className="relative flex h-11 w-11 items-center justify-center overflow-hidden rounded-2xl bg-ink text-white shadow-lg shadow-ink/20">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,_rgba(251,191,36,0.35),_transparent_45%),linear-gradient(160deg,_rgba(255,255,255,0.2),_transparent_55%)]" />
        <span className="relative font-display text-lg font-bold tracking-tight">PP</span>
      </div>
      {!compact ? (
        <div>
          <p className="font-display text-base font-bold tracking-tight text-slate-950">PriceProof SA</p>
          <p className="text-xs text-slate-600">Receipt-backed pricing evidence</p>
        </div>
      ) : null}
    </div>
  );
}
