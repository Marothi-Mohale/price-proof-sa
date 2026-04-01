"use client";

import { useEffect, useState } from "react";
import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { formatFileSize } from "@/lib/utils";

export function SelectedFilePreview({ file, title }: { file: File | null; title: string }) {
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!file || !file.type.startsWith("image/")) {
      setPreviewUrl(null);
      return;
    }

    const nextPreviewUrl = URL.createObjectURL(file);
    setPreviewUrl(nextPreviewUrl);

    return () => URL.revokeObjectURL(nextPreviewUrl);
  }, [file]);

  if (!file) {
    return null;
  }

  return (
    <Card className="space-y-3 border-slate-200">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <p className="text-sm font-semibold text-slate-900">{title}</p>
          <p className="text-sm text-slate-600">{file.name}</p>
        </div>
        <Badge tone="info">{formatFileSize(file.size)}</Badge>
      </div>
      {previewUrl ? (
        // eslint-disable-next-line @next/next/no-img-element
        <img src={previewUrl} alt={file.name} className="h-44 w-full rounded-2xl object-cover" />
      ) : (
        <div className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-6 text-sm text-slate-500">
          Preview is available after upload for image files. This file will still be attached to the case.
        </div>
      )}
    </Card>
  );
}
