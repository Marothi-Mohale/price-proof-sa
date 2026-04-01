import type { ReactNode } from "react";

export function PageHeader({
  eyebrow,
  title,
  description,
  actions
}: {
  eyebrow?: string;
  title: string;
  description: string;
  actions?: ReactNode;
}) {
  return (
    <div className="flex flex-col gap-4 rounded-[32px] border border-white/70 bg-[linear-gradient(135deg,rgba(255,255,255,0.96),rgba(255,248,235,0.86))] p-6 shadow-[0_18px_45px_rgba(15,23,42,0.08)]">
      <div className="space-y-2">
        {eyebrow ? <p className="text-xs font-semibold uppercase tracking-[0.22em] text-amber-700">{eyebrow}</p> : null}
        <h1 className="font-display text-3xl font-bold tracking-tight text-slate-950 sm:text-4xl">{title}</h1>
        <p className="max-w-3xl text-sm leading-6 text-slate-600 sm:text-base">{description}</p>
      </div>
      {actions ? <div className="flex flex-wrap gap-3">{actions}</div> : null}
    </div>
  );
}
