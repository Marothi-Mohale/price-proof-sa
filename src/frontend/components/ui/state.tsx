import type { ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";

export function LoadingState({
  title = "Loading",
  message = "Fetching the latest information."
}: {
  title?: string;
  message?: string;
}) {
  return (
    <Card className="flex min-h-[220px] flex-col items-center justify-center gap-4 text-center">
      <div className="h-10 w-10 animate-spin rounded-full border-4 border-slate-200 border-t-amber-500" />
      <div className="space-y-2">
        <CardTitle>{title}</CardTitle>
        <CardDescription>{message}</CardDescription>
      </div>
    </Card>
  );
}

export function ErrorState({
  title = "Something went wrong",
  message,
  actionLabel,
  onAction
}: {
  title?: string;
  message: string;
  actionLabel?: string;
  onAction?: () => void;
}) {
  return (
    <Card className="space-y-4 border-rose-200 bg-rose-50/80">
      <div className="space-y-2">
        <CardTitle className="text-rose-900">{title}</CardTitle>
        <CardDescription className="text-rose-800">{message}</CardDescription>
      </div>
      {actionLabel && onAction ? <Button onClick={onAction}>{actionLabel}</Button> : null}
    </Card>
  );
}

export function EmptyState({
  title,
  description,
  action
}: {
  title: string;
  description: string;
  action?: ReactNode;
}) {
  return (
    <Card className="space-y-4 text-center">
      <div className="space-y-2">
        <CardTitle>{title}</CardTitle>
        <CardDescription>{description}</CardDescription>
      </div>
      {action}
    </Card>
  );
}
