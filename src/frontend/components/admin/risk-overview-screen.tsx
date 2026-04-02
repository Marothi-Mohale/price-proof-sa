"use client";

import { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { formatDateTime } from "@/lib/format";
import type { RiskLabel, RiskOverview } from "@/lib/types";
import { useSession } from "@/components/providers/session-provider";
import { Badge } from "@/components/ui/badge";
import { ButtonLink } from "@/components/ui/button-link";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { PageHeader } from "@/components/ui/page-header";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/state";

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

export function RiskOverviewScreen() {
  const { session, currentUser } = useSession();
  const [overview, setOverview] = useState<RiskOverview | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const isAdmin = currentUser?.isAdmin ?? session?.isAdmin ?? false;
  const accessToken = session?.accessToken ?? "";

  useEffect(() => {
    if (!session || !isAdmin || !accessToken) {
      setLoading(false);
      return;
    }

    let isCancelled = false;

    async function loadOverview() {
      setLoading(true);
      setError(null);

      try {
        const result = await api.getRiskOverview(accessToken);

        if (!isCancelled) {
          setOverview(result);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setError(loadError instanceof Error ? loadError.message : "Unable to load the risk overview.");
        }
      } finally {
        if (!isCancelled) {
          setLoading(false);
        }
      }
    }

    void loadOverview();

    return () => {
      isCancelled = true;
    };
  }, [accessToken, isAdmin, session]);

  if (!isAdmin) {
    return (
      <div className="space-y-6">
        <PageHeader
          eyebrow="Admin"
          title="Risk desk"
          description="This page is limited to PriceProof SA administrators who review cross-merchant risk signals."
          actions={<ButtonLink href="/dashboard" variant="secondary">Back to dashboard</ButtonLink>}
        />
        <ErrorState title="Admin access required" message="Sign in with an administrator account to view merchant and branch risk rankings." />
      </div>
    );
  }

  if (loading) {
    return <LoadingState title="Loading risk desk" message="Ranking merchants and branches by the latest conservative risk score." />;
  }

  if (error || !overview) {
    return <ErrorState message={error ?? "Risk overview is unavailable."} actionLabel="Try again" onAction={() => window.location.reload()} />;
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Admin"
        title="Risk desk"
        description="Review merchants and branches with repeated high-confidence mismatch signals so the team can spot patterns early."
        actions={<ButtonLink href="/dashboard" variant="secondary">Back to dashboard</ButtonLink>}
      />

      {overview.topMerchants.length === 0 && overview.topBranches.length === 0 ? (
        <EmptyState
          title="No risk signals yet"
          description="Risk scores will appear here after cases are analyzed and merchant or branch snapshots are generated."
        />
      ) : null}

      <div className="grid gap-6 xl:grid-cols-2">
        <Card className="space-y-4">
          <div className="space-y-1">
            <CardTitle>Top risky merchants</CardTitle>
            <CardDescription>Latest merchant-level scores, sorted from highest to lowest.</CardDescription>
          </div>
          <div className="grid gap-3">
            {overview.topMerchants.map((merchant) => (
              <div key={merchant.merchantId} className="rounded-3xl border border-slate-200 p-4">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                  <div className="space-y-1">
                    <p className="text-base font-semibold text-slate-950">{merchant.merchantName}</p>
                    <p className="text-sm text-slate-600">
                      {merchant.category ?? "Category not recorded"} • {merchant.totalCases} total cases
                    </p>
                    <p className="text-xs uppercase tracking-[0.18em] text-slate-500">
                      {merchant.likelyCardSurchargeCases} likely surcharge signal{merchant.likelyCardSurchargeCases === 1 ? "" : "s"}
                    </p>
                  </div>
                  <div className="flex items-center gap-3">
                    <div className="text-right">
                      <p className="font-display text-2xl font-semibold text-slate-950">{merchant.score.toFixed(2)}</p>
                      <p className="text-xs text-slate-500">{formatDateTime(merchant.calculatedUtc)}</p>
                    </div>
                    <Badge tone={riskTone(merchant.label)}>{merchant.label}</Badge>
                  </div>
                </div>
                <div className="mt-4">
                  <ButtonLink href={`/merchants/${merchant.merchantId}`}>Open merchant profile</ButtonLink>
                </div>
              </div>
            ))}
          </div>
        </Card>

        <Card className="space-y-4">
          <div className="space-y-1">
            <CardTitle>Top risky branches</CardTitle>
            <CardDescription>Branch-level hotspots, useful when a merchant-wide score hides localized patterns.</CardDescription>
          </div>
          <div className="grid gap-3">
            {overview.topBranches.map((branch) => (
              <div key={branch.branchId} className="rounded-3xl border border-slate-200 p-4">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                  <div className="space-y-1">
                    <p className="text-base font-semibold text-slate-950">{branch.branchName}</p>
                    <p className="text-sm text-slate-600">
                      {branch.merchantName} • {branch.city}, {branch.province}
                    </p>
                    <p className="text-xs uppercase tracking-[0.18em] text-slate-500">
                      {branch.likelyCardSurchargeCases} likely surcharge signal{branch.likelyCardSurchargeCases === 1 ? "" : "s"} across {branch.totalCases} cases
                    </p>
                  </div>
                  <div className="flex items-center gap-3">
                    <div className="text-right">
                      <p className="font-display text-2xl font-semibold text-slate-950">{branch.score.toFixed(2)}</p>
                      <p className="text-xs text-slate-500">{formatDateTime(branch.calculatedUtc)}</p>
                    </div>
                    <Badge tone={riskTone(branch.label)}>{branch.label}</Badge>
                  </div>
                </div>
                <div className="mt-4">
                  <ButtonLink href={`/merchants/${branch.merchantId}`} variant="secondary">View merchant context</ButtonLink>
                </div>
              </div>
            ))}
          </div>
        </Card>
      </div>
    </div>
  );
}
