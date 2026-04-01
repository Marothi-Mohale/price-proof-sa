"use client";

import { useEffect, useState } from "react";
import { api, caseClassificationOptions } from "@/lib/api";
import { formatCurrency, formatDateTime, humanizeCode } from "@/lib/format";
import type { BootstrapLookups, CaseClassification, CaseSummary } from "@/lib/types";
import { useSession } from "@/components/providers/session-provider";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ButtonLink } from "@/components/ui/button-link";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { FieldShell, SelectInput } from "@/components/ui/field";
import { PageHeader } from "@/components/ui/page-header";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/state";

function classificationTone(classification: string) {
  if (classification === "Match") {
    return "success";
  }

  if (classification === "PotentialCardSurcharge" || classification === "Overcharge") {
    return "danger";
  }

  if (classification === "NeedsReview" || classification === "PendingEvidence") {
    return "warning";
  }

  return "info";
}

export function DashboardScreen() {
  const { session } = useSession();
  const [lookups, setLookups] = useState<BootstrapLookups | null>(null);
  const [cases, setCases] = useState<CaseSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedMerchantId, setSelectedMerchantId] = useState("");
  const [selectedClassification, setSelectedClassification] = useState<CaseClassification | "">("");

  useEffect(() => {
    if (!session) {
      return;
    }

    let isCancelled = false;
    const activeUserId = session.userId;

    async function loadDashboard() {
      setLoading(true);
      setError(null);

      try {
        const [bootstrap, casePage] = await Promise.all([
          api.getBootstrapLookups(),
          api.listCases({
            reportedByUserId: activeUserId,
            merchantId: selectedMerchantId || undefined,
            classification: selectedClassification || undefined,
            take: 50
          })
        ]);

        if (!isCancelled) {
          setLookups(bootstrap);
          setCases(casePage.items);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setError(loadError instanceof Error ? loadError.message : "Unable to load your dashboard.");
        }
      } finally {
        if (!isCancelled) {
          setLoading(false);
        }
      }
    }

    void loadDashboard();

    return () => {
      isCancelled = true;
    };
  }, [selectedClassification, selectedMerchantId, session]);

  const totalOpenCases = cases.filter((item) => item.status !== "Closed").length;
  const mismatches = cases.filter((item) => item.differenceAmount && item.differenceAmount > 0).length;
  const matches = cases.filter((item) => item.classification === "Match").length;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Workspace"
        title="Dashboard"
        description="Track active discrepancy cases, revisit merchant history, and move quickly into a new capture flow."
        actions={<ButtonLink href="/cases/new">Start new case</ButtonLink>}
      />

      <div className="grid gap-4 sm:grid-cols-3">
        <Card>
          <p className="text-sm text-slate-500">Open cases</p>
          <p className="mt-3 font-display text-3xl font-semibold text-slate-950">{totalOpenCases}</p>
        </Card>
        <Card>
          <p className="text-sm text-slate-500">Positive mismatches</p>
          <p className="mt-3 font-display text-3xl font-semibold text-slate-950">{mismatches}</p>
        </Card>
        <Card>
          <p className="text-sm text-slate-500">Confirmed matches</p>
          <p className="mt-3 font-display text-3xl font-semibold text-slate-950">{matches}</p>
        </Card>
      </div>

      <Card className="space-y-4">
        <div className="grid gap-4 md:grid-cols-2">
          <FieldShell htmlFor="merchant-filter" label="Merchant filter">
            <SelectInput
              id="merchant-filter"
              value={selectedMerchantId}
              onChange={(event) => setSelectedMerchantId(event.target.value)}
            >
              <option value="">All merchants</option>
              {lookups?.merchants.map((merchant) => (
                <option key={merchant.id} value={merchant.id}>
                  {merchant.name}
                </option>
              ))}
            </SelectInput>
          </FieldShell>
          <FieldShell htmlFor="classification-filter" label="Classification filter">
            <SelectInput
              id="classification-filter"
              value={selectedClassification}
              onChange={(event) => setSelectedClassification(event.target.value as CaseClassification | "")}
            >
              <option value="">All classifications</option>
              {caseClassificationOptions.map((classification) => (
                <option key={classification} value={classification}>
                  {humanizeCode(classification)}
                </option>
              ))}
            </SelectInput>
          </FieldShell>
        </div>
      </Card>

      {loading ? <LoadingState title="Loading dashboard" message="Pulling the latest cases and lookups." /> : null}
      {!loading && error ? <ErrorState message={error} actionLabel="Try again" onAction={() => window.location.reload()} /> : null}

      {!loading && !error && cases.length === 0 ? (
        <EmptyState
          title="No cases yet"
          description="Start your first case to capture a quoted price, upload the payment evidence, and prepare a complaint pack."
          action={<ButtonLink href="/cases/new">Start new case</ButtonLink>}
        />
      ) : null}

      {!loading && !error && cases.length > 0 ? (
        <div className="grid gap-4">
          {cases.map((item) => (
            <Card key={item.id} className="space-y-4">
              <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">{item.caseNumber}</p>
                  <CardTitle className="mt-2">{item.merchant.name}</CardTitle>
                  <CardDescription>
                    {item.branch?.name ? `${item.branch.name}, ` : ""}
                    {item.basketDescription}
                  </CardDescription>
                </div>
                <Badge tone={classificationTone(item.classification)}>{humanizeCode(item.classification)}</Badge>
              </div>

              <div className="grid gap-3 sm:grid-cols-3">
                <div className="rounded-2xl bg-slate-50 p-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Quoted</p>
                  <p className="mt-2 text-sm font-semibold text-slate-950">{formatCurrency(item.latestQuotedAmount, "ZAR")}</p>
                </div>
                <div className="rounded-2xl bg-slate-50 p-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Charged</p>
                  <p className="mt-2 text-sm font-semibold text-slate-950">{formatCurrency(item.latestPaidAmount, "ZAR")}</p>
                </div>
                <div className="rounded-2xl bg-slate-50 p-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Updated</p>
                  <p className="mt-2 text-sm font-semibold text-slate-950">{formatDateTime(item.updatedUtc)}</p>
                </div>
              </div>

              <div className="flex flex-wrap gap-3">
                <ButtonLink href={`/cases/${item.id}`}>View case</ButtonLink>
                <ButtonLink href={`/merchants/${item.merchant.id}`} variant="secondary">Merchant history</ButtonLink>
              </div>
            </Card>
          ))}
        </div>
      ) : null}
    </div>
  );
}
