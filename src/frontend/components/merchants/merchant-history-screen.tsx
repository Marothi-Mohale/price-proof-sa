"use client";

import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { formatCurrency, formatDateTime, formatPercentage, humanizeCode } from "@/lib/format";
import type { MerchantHistory, MerchantRisk, RiskLabel } from "@/lib/types";
import { Badge } from "@/components/ui/badge";
import { ButtonLink } from "@/components/ui/button-link";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { PageHeader } from "@/components/ui/page-header";
import { ErrorState, LoadingState } from "@/components/ui/state";

function riskTone(label: RiskLabel) {
  if (label === "Severe") {
    return "danger";
  }

  if (label === "High") {
    return "warning";
  }

  if (label === "Moderate") {
    return "info";
  }

  return "success";
}

export function MerchantHistoryScreen({ merchantId }: { merchantId: string }) {
  const [history, setHistory] = useState<MerchantHistory | null>(null);
  const [risk, setRisk] = useState<MerchantRisk | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isCancelled = false;

    async function loadHistory() {
      setLoading(true);
      setError(null);

      try {
        const [result, riskResult] = await Promise.all([
          api.getMerchantHistory(merchantId),
          api.getMerchantRisk(merchantId)
        ]);

        if (!isCancelled) {
          setHistory(result);
          setRisk(riskResult);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setError(loadError instanceof Error ? loadError.message : "Unable to load merchant history.");
        }
      } finally {
        if (!isCancelled) {
          setLoading(false);
        }
      }
    }

    void loadHistory();
    return () => {
      isCancelled = true;
    };
  }, [merchantId]);

  if (loading) {
    return <LoadingState title="Loading merchant history" message="Summarizing recent cases and classification signals." />;
  }

  if (error || !history) {
    return <ErrorState message={error ?? "Merchant history is unavailable."} actionLabel="Try again" onAction={() => window.location.reload()} />;
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Merchant profile"
        title={history.merchantName}
        description={history.websiteUrl ?? history.category ?? "Recent PriceProof SA case history for this merchant."}
        actions={<ButtonLink href="/dashboard" variant="secondary">Back to dashboard</ButtonLink>}
      />

      <div className="grid gap-4 sm:grid-cols-4">
        <Card><p className="text-sm text-slate-500">Total cases</p><p className="mt-3 font-display text-3xl font-semibold text-slate-950">{history.totalCases}</p></Card>
        <Card><p className="text-sm text-slate-500">Potential surcharge</p><p className="mt-3 font-display text-3xl font-semibold text-slate-950">{history.potentialCardSurchargeCases}</p></Card>
        <Card><p className="text-sm text-slate-500">Needs review</p><p className="mt-3 font-display text-3xl font-semibold text-slate-950">{history.needsReviewCases}</p></Card>
        <Card><p className="text-sm text-slate-500">Matches</p><p className="mt-3 font-display text-3xl font-semibold text-slate-950">{history.matchCases}</p></Card>
      </div>

      {risk ? (
        <Card className="space-y-5">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
            <div className="space-y-2">
              <CardTitle>Merchant risk score</CardTitle>
              <CardDescription>
                A conservative pattern score based on analyzed cases, weighted mismatch totals, and how recent the
                signals are.
              </CardDescription>
            </div>
            <Badge tone={riskTone(risk.label)}>{risk.label}</Badge>
          </div>

          <div className="grid gap-4 md:grid-cols-4">
            <div className="rounded-3xl bg-slate-50 p-4">
              <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Score</p>
              <p className="mt-2 font-display text-3xl font-semibold text-slate-950">{risk.score.toFixed(2)}</p>
            </div>
            <div className="rounded-3xl bg-slate-50 p-4">
              <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Analyzed cases</p>
              <p className="mt-2 text-lg font-semibold text-slate-950">{risk.analyzedCases}</p>
            </div>
            <div className="rounded-3xl bg-slate-50 p-4">
              <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Weighted mismatch</p>
              <p className="mt-2 text-lg font-semibold text-slate-950">{formatCurrency(risk.confidenceWeightedMismatchTotal, "ZAR")}</p>
            </div>
            <div className="rounded-3xl bg-slate-50 p-4">
              <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Last calculated</p>
              <p className="mt-2 text-lg font-semibold text-slate-950">{formatDateTime(risk.calculatedUtc)}</p>
            </div>
          </div>

          <div className="grid gap-4 md:grid-cols-3">
            <div className="rounded-3xl border border-slate-200 p-4">
              <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Likely surcharge ratio</p>
              <p className="mt-2 text-sm font-semibold text-slate-950">
                {risk.totalCases === 0 ? "0 / 0" : `${risk.likelyCardSurchargeCases} / ${risk.totalCases}`}
              </p>
            </div>
            <div className="rounded-3xl border border-slate-200 p-4">
              <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Dismissed equivalent ratio</p>
              <p className="mt-2 text-sm font-semibold text-slate-950">{formatPercentage(risk.dismissedEquivalentRatio * 100)}</p>
            </div>
            <div className="rounded-3xl border border-slate-200 p-4">
              <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Unclear ratio</p>
              <p className="mt-2 text-sm font-semibold text-slate-950">{formatPercentage(risk.unclearCaseRatio * 100)}</p>
            </div>
          </div>
        </Card>
      ) : null}

      <Card className="space-y-4">
        <CardTitle>Recent cases</CardTitle>
        <div className="grid gap-4">
          {history.recentCases.map((item) => (
            <div key={item.id} className="rounded-3xl border border-slate-200 p-4">
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">{item.caseNumber}</p>
                  <p className="mt-2 text-base font-semibold text-slate-950">{item.basketDescription}</p>
                  <p className="text-sm text-slate-600">{item.branch?.name ?? "Branch not recorded"} - {formatDateTime(item.updatedUtc)}</p>
                </div>
                <div className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-700">{humanizeCode(item.classification)}</div>
              </div>
              <div className="mt-4">
                <ButtonLink href={`/cases/${item.id}`}>Open case</ButtonLink>
              </div>
            </div>
          ))}
        </div>
        {history.recentCases.length === 0 ? <CardDescription>No tracked cases yet for this merchant.</CardDescription> : null}
      </Card>
    </div>
  );
}
