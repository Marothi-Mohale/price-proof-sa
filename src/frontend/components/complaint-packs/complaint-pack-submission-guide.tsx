"use client";

import { useState } from "react";
import type { ComplaintPackSubmissionGuidance } from "@/lib/types";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";

type ComplaintPackSubmissionGuideProps = {
  guidance: ComplaintPackSubmissionGuidance;
};

export function ComplaintPackSubmissionGuide({ guidance }: ComplaintPackSubmissionGuideProps) {
  const [copyMessage, setCopyMessage] = useState<string | null>(null);

  async function copyTemplate() {
    const combinedTemplate = `Subject: ${guidance.emailTemplate.subject}\n\n${guidance.emailTemplate.body}`;

    try {
      await navigator.clipboard.writeText(combinedTemplate);
      setCopyMessage("Email template copied.");
    } catch {
      setCopyMessage("Copy failed. You can still select and copy the template manually.");
    }
  }

  return (
    <Card className="space-y-4">
      <div className="space-y-2">
        <CardTitle>Where to send this complaint pack</CardTitle>
        <CardDescription>{guidance.safeUseNote}</CardDescription>
      </div>

      <div className="grid gap-3">
        {guidance.recommendedRoutes.map((route) => (
          <div key={`${route.order}-${route.channel}`} className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
            <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Step {route.order}</p>
            <p className="mt-2 font-display text-lg font-semibold text-slate-950">{route.channel}</p>
            <p className="mt-1 text-sm font-semibold text-slate-700">{route.recipient}</p>
            <p className="mt-2 text-sm leading-6 text-slate-700">{route.reason}</p>
            <p className="mt-2 text-sm text-slate-600">{route.whenToUse}</p>
          </div>
        ))}
      </div>

      <div className="space-y-3 rounded-3xl border border-slate-200 p-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <p className="text-sm font-semibold text-slate-950">Ready-to-send email template</p>
            <p className="text-sm text-slate-600">Copy this template and attach the generated complaint pack PDF.</p>
          </div>
          <Button variant="secondary" onClick={copyTemplate}>Copy email template</Button>
        </div>

        <div className="rounded-2xl bg-slate-50 p-4">
          <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Subject</p>
          <p className="mt-2 text-sm font-semibold text-slate-950">{guidance.emailTemplate.subject}</p>
        </div>

        <div className="rounded-2xl bg-slate-50 p-4">
          <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Body</p>
          <pre className="mt-2 whitespace-pre-wrap font-body text-sm leading-6 text-slate-700">
            {guidance.emailTemplate.body}
          </pre>
        </div>

        {copyMessage ? <p className="text-sm text-slate-600">{copyMessage}</p> : null}
      </div>
    </Card>
  );
}
