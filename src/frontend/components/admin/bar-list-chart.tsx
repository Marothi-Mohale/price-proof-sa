import { Badge } from "@/components/ui/badge";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";

export type BarListChartItem = {
  id: string;
  label: string;
  value: number;
  valueLabel: string;
  description?: string;
  badgeLabel?: string;
  badgeTone?: "neutral" | "success" | "warning" | "danger" | "info";
};

export function BarListChart({
  title,
  description,
  items,
  accentClassName = "bg-amber-500",
  emptyMessage
}: {
  title: string;
  description: string;
  items: BarListChartItem[];
  accentClassName?: string;
  emptyMessage: string;
}) {
  const maxValue = items.reduce((largest, item) => Math.max(largest, item.value), 0);

  return (
    <Card className="space-y-5">
      <div className="space-y-1">
        <CardTitle>{title}</CardTitle>
        <CardDescription>{description}</CardDescription>
      </div>

      {items.length === 0 ? (
        <div className="rounded-3xl border border-dashed border-slate-200 px-4 py-8 text-sm text-slate-500">
          {emptyMessage}
        </div>
      ) : (
        <div className="space-y-4">
          {items.map((item) => {
            const widthPercent = maxValue <= 0 ? 0 : Math.max(6, (item.value / maxValue) * 100);

            return (
              <div key={item.id} className="space-y-2">
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0 space-y-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <p className="truncate text-sm font-semibold text-slate-950">{item.label}</p>
                      {item.badgeLabel ? <Badge tone={item.badgeTone ?? "neutral"}>{item.badgeLabel}</Badge> : null}
                    </div>
                    {item.description ? <p className="text-xs leading-5 text-slate-500">{item.description}</p> : null}
                  </div>
                  <p className="shrink-0 text-sm font-semibold text-slate-950">{item.valueLabel}</p>
                </div>
                <div className="h-2.5 rounded-full bg-slate-100">
                  <div className={`h-2.5 rounded-full ${accentClassName}`} style={{ width: `${widthPercent}%` }} />
                </div>
              </div>
            );
          })}
        </div>
      )}
    </Card>
  );
}
