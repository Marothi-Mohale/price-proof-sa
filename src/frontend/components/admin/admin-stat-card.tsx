import { Card, CardDescription, CardTitle } from "@/components/ui/card";

export function AdminStatCard({
  label,
  value,
  helperText
}: {
  label: string;
  value: string;
  helperText: string;
}) {
  return (
    <Card className="space-y-3">
      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">{label}</p>
      <CardTitle className="font-display text-3xl">{value}</CardTitle>
      <CardDescription>{helperText}</CardDescription>
    </Card>
  );
}
