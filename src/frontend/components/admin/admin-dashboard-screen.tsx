"use client";

import Link from "next/link";
import { type FormEvent, type ReactNode, useEffect, useState } from "react";
import { AdminStatCard } from "@/components/admin/admin-stat-card";
import { BarListChart } from "@/components/admin/bar-list-chart";
import { useSession } from "@/components/providers/session-provider";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ButtonLink } from "@/components/ui/button-link";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { FieldShell, SelectInput, TextInput } from "@/components/ui/field";
import { PageHeader } from "@/components/ui/page-header";
import { EmptyState, ErrorState, LoadingState } from "@/components/ui/state";
import { api, buildUploadContentUrl } from "@/lib/api";
import { formatDateTime, formatPercentage, humanizeCode } from "@/lib/format";
import { adminDashboardFilterSchema, flattenZodErrors } from "@/lib/validators";
import type {
  AdminBranchRiskRow,
  AdminDashboardFilter,
  AdminDashboardSummary,
  AdminMerchantRiskRow,
  BootstrapLookups,
  PagedResult,
  RecentUpload,
  RiskLabel
} from "@/lib/types";

const TABLE_PAGE_SIZE = 8;

type FilterFormState = {
  fromDate: string;
  toDate: string;
  province: string;
  city: string;
};

const emptyFilters: FilterFormState = {
  fromDate: "",
  toDate: "",
  province: "",
  city: ""
};

function riskTone(label: string): "success" | "warning" | "danger" | "info" {
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

function toFilterQuery(filters: FilterFormState): AdminDashboardFilter {
  return {
    fromDate: filters.fromDate || undefined,
    toDate: filters.toDate || undefined,
    province: filters.province || undefined,
    city: filters.city || undefined
  };
}

function formatWholeNumber(value: number) {
  return value.toLocaleString("en-ZA");
}

function getPageCount(totalCount: number, pageSize: number) {
  return Math.max(1, Math.ceil(totalCount / pageSize));
}

function getProvinceOptions(lookups: BootstrapLookups | null) {
  const values = new Set<string>();

  lookups?.merchants.forEach((merchant) => {
    merchant.branches.forEach((branch) => {
      if (branch.province) {
        values.add(branch.province);
      }
    });
  });

  return Array.from(values).sort((left, right) => left.localeCompare(right));
}

function getCityOptions(lookups: BootstrapLookups | null, province: string) {
  const values = new Set<string>();

  lookups?.merchants.forEach((merchant) => {
    merchant.branches.forEach((branch) => {
      if ((!province || branch.province === province) && branch.city) {
        values.add(branch.city);
      }
    });
  });

  return Array.from(values).sort((left, right) => left.localeCompare(right));
}

function TablePager({
  pageIndex,
  pageSize,
  totalCount,
  onPrevious,
  onNext
}: {
  pageIndex: number;
  pageSize: number;
  totalCount: number;
  onPrevious: () => void;
  onNext: () => void;
}) {
  const pageCount = getPageCount(totalCount, pageSize);

  return (
    <div className="flex flex-col gap-3 border-t border-slate-100 pt-4 sm:flex-row sm:items-center sm:justify-between">
      <p className="text-sm text-slate-500">
        Page {pageIndex + 1} of {pageCount} - {formatWholeNumber(totalCount)} total
      </p>
      <div className="flex gap-2">
        <Button variant="secondary" onClick={onPrevious} disabled={pageIndex <= 0}>
          Previous
        </Button>
        <Button variant="secondary" onClick={onNext} disabled={(pageIndex + 1) * pageSize >= totalCount}>
          Next
        </Button>
      </div>
    </div>
  );
}

function TableShell({
  title,
  description,
  children,
  footer
}: {
  title: string;
  description: string;
  children: ReactNode;
  footer?: ReactNode;
}) {
  return (
    <Card className="space-y-5">
      <div className="space-y-1">
        <CardTitle>{title}</CardTitle>
        <CardDescription>{description}</CardDescription>
      </div>
      <div className="overflow-x-auto">{children}</div>
      {footer}
    </Card>
  );
}

export function AdminDashboardScreen() {
  const { session, currentUser } = useSession();
  const isAdmin = currentUser?.isAdmin ?? session?.isAdmin ?? false;

  const [lookups, setLookups] = useState<BootstrapLookups | null>(null);
  const [summary, setSummary] = useState<AdminDashboardSummary | null>(null);
  const [merchantRows, setMerchantRows] = useState<PagedResult<AdminMerchantRiskRow> | null>(null);
  const [branchRows, setBranchRows] = useState<PagedResult<AdminBranchRiskRow> | null>(null);
  const [uploadRows, setUploadRows] = useState<PagedResult<RecentUpload> | null>(null);
  const [draftFilters, setDraftFilters] = useState<FilterFormState>(emptyFilters);
  const [appliedFilters, setAppliedFilters] = useState<FilterFormState>(emptyFilters);
  const [filterErrors, setFilterErrors] = useState<Record<string, string>>({});
  const [merchantPageIndex, setMerchantPageIndex] = useState(0);
  const [branchPageIndex, setBranchPageIndex] = useState(0);
  const [uploadPageIndex, setUploadPageIndex] = useState(0);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const provinceOptions = getProvinceOptions(lookups);
  const cityOptions = getCityOptions(lookups, draftFilters.province);

  useEffect(() => {
    if (!session) {
      return;
    }

    let isCancelled = false;

    async function loadLookups() {
      try {
        const result = await api.getBootstrapLookups();

        if (!isCancelled) {
          setLookups(result);
        }
      } catch {
      }
    }

    void loadLookups();

    return () => {
      isCancelled = true;
    };
  }, [session]);

  useEffect(() => {
    if (!session || !isAdmin) {
      setLoading(false);
      return;
    }

    let isCancelled = false;
    const query = toFilterQuery(appliedFilters);

    async function loadDashboard() {
      setError(null);

      if (!summary) {
        setLoading(true);
      } else {
        setRefreshing(true);
      }

      try {
        const [summaryResult, merchantResult, branchResult, uploadResult] = await Promise.all([
          api.getAdminDashboardSummary(query),
          api.getAdminTopMerchants({
            ...query,
            skip: merchantPageIndex * TABLE_PAGE_SIZE,
            take: TABLE_PAGE_SIZE
          }),
          api.getAdminTopBranches({
            ...query,
            skip: branchPageIndex * TABLE_PAGE_SIZE,
            take: TABLE_PAGE_SIZE
          }),
          api.getAdminRecentUploads({
            ...query,
            skip: uploadPageIndex * TABLE_PAGE_SIZE,
            take: TABLE_PAGE_SIZE
          })
        ]);

        if (!isCancelled) {
          setSummary(summaryResult);
          setMerchantRows(merchantResult);
          setBranchRows(branchResult);
          setUploadRows(uploadResult);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setError(loadError instanceof Error ? loadError.message : "Unable to load the admin dashboard.");
        }
      } finally {
        if (!isCancelled) {
          setLoading(false);
          setRefreshing(false);
        }
      }
    }

    void loadDashboard();

    return () => {
      isCancelled = true;
    };
  }, [appliedFilters, branchPageIndex, isAdmin, merchantPageIndex, session, uploadPageIndex]);

  async function handleExport() {
    setExporting(true);
    setError(null);

    try {
      await api.downloadAdminDashboardCsv(toFilterQuery(appliedFilters));
    } catch (exportError) {
      setError(exportError instanceof Error ? exportError.message : "Unable to export the admin report.");
    } finally {
      setExporting(false);
    }
  }

  function handleApplyFilters(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const result = adminDashboardFilterSchema.safeParse(draftFilters);

    if (!result.success) {
      setFilterErrors(flattenZodErrors(result.error.issues));
      return;
    }

    setFilterErrors({});
    setAppliedFilters({
      fromDate: result.data.fromDate ?? "",
      toDate: result.data.toDate ?? "",
      province: result.data.province ?? "",
      city: result.data.city ?? ""
    });
    setMerchantPageIndex(0);
    setBranchPageIndex(0);
    setUploadPageIndex(0);
  }

  function handleResetFilters() {
    setDraftFilters(emptyFilters);
    setAppliedFilters(emptyFilters);
    setFilterErrors({});
    setMerchantPageIndex(0);
    setBranchPageIndex(0);
    setUploadPageIndex(0);
  }

  if (!isAdmin) {
    return (
      <div className="space-y-6">
        <PageHeader
          eyebrow="Admin"
          title="Operations dashboard"
          description="This workspace is limited to PriceProof SA administrators reviewing merchant and branch patterns."
          actions={<ButtonLink href="/dashboard" variant="secondary">Back to dashboard</ButtonLink>}
        />
        <ErrorState
          title="Admin access required"
          message="Sign in with an administrator account to review reports, OCR outcomes, and merchant risk signals."
        />
      </div>
    );
  }

  if (loading && !summary) {
    return <LoadingState title="Loading admin dashboard" message="Aggregating merchant risk, OCR outcomes, and recent uploads." />;
  }

  if (!loading && !summary) {
    return (
      <ErrorState
        message={error ?? "Admin dashboard data is unavailable."}
        actionLabel="Reload"
        onAction={() => window.location.reload()}
      />
    );
  }

  const casesByClassification = summary?.casesByClassification ?? [];
  const topMerchantChartRows = summary?.topMerchants ?? [];
  const topBranchChartRows = summary?.topBranches ?? [];
  const hasAnyData = (summary?.totalCases ?? 0) > 0;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Admin"
        title="Operations dashboard"
        description="Monitor live case volumes, compare classifications, and spot risky merchant patterns without turning the review workflow into a complex BI tool."
        actions={
          <>
            <Button variant="secondary" onClick={handleResetFilters}>
              Clear filters
            </Button>
            <Button onClick={handleExport} busy={exporting}>
              Export CSV
            </Button>
          </>
        }
      />

      <Card className="space-y-4">
        <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
          <div className="space-y-1">
            <CardTitle>Filter report window</CardTitle>
            <CardDescription>Use a date range and branch location to narrow the reporting slice.</CardDescription>
          </div>
          {refreshing ? <Badge tone="info">Refreshing</Badge> : null}
        </div>

        <form className="grid gap-4 md:grid-cols-2 xl:grid-cols-4" onSubmit={handleApplyFilters}>
          <FieldShell htmlFor="from-date" label="From date" error={filterErrors.fromDate}>
            <TextInput
              id="from-date"
              type="date"
              value={draftFilters.fromDate}
              onChange={(event) => setDraftFilters((current) => ({ ...current, fromDate: event.target.value }))}
            />
          </FieldShell>

          <FieldShell htmlFor="to-date" label="To date" error={filterErrors.toDate}>
            <TextInput
              id="to-date"
              type="date"
              value={draftFilters.toDate}
              onChange={(event) => setDraftFilters((current) => ({ ...current, toDate: event.target.value }))}
            />
          </FieldShell>

          <FieldShell htmlFor="province-filter" label="Region / province" error={filterErrors.province}>
            <SelectInput
              id="province-filter"
              value={draftFilters.province}
              onChange={(event) => {
                const nextProvince = event.target.value;
                const nextCityOptions = getCityOptions(lookups, nextProvince);

                setDraftFilters((current) => ({
                  ...current,
                  province: nextProvince,
                  city: nextCityOptions.includes(current.city) ? current.city : ""
                }));
              }}
            >
              <option value="">All regions</option>
              {provinceOptions.map((province) => (
                <option key={province} value={province}>
                  {province}
                </option>
              ))}
            </SelectInput>
          </FieldShell>

          <FieldShell htmlFor="city-filter" label="City" error={filterErrors.city}>
            <SelectInput
              id="city-filter"
              value={draftFilters.city}
              onChange={(event) => setDraftFilters((current) => ({ ...current, city: event.target.value }))}
            >
              <option value="">All cities</option>
              {cityOptions.map((city) => (
                <option key={city} value={city}>
                  {city}
                </option>
              ))}
            </SelectInput>
          </FieldShell>

          <div className="flex flex-wrap gap-3 md:col-span-2 xl:col-span-4">
            <Button type="submit">Apply filters</Button>
            <Button type="button" variant="ghost" onClick={handleResetFilters}>
              Reset view
            </Button>
          </div>
        </form>
      </Card>

      {error ? <ErrorState message={error} /> : null}

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <AdminStatCard
          label="Total cases"
          value={formatWholeNumber(summary?.totalCases ?? 0)}
          helperText="Cases that match the current reporting window."
        />
        <AdminStatCard
          label="Unresolved cases"
          value={formatWholeNumber(summary?.unresolvedCases ?? 0)}
          helperText="Cases still open, awaiting payment, or pending review."
        />
        <AdminStatCard
          label="OCR success rate"
          value={formatPercentage(summary?.ocrSuccessRate ?? 0)}
          helperText={`${formatWholeNumber(summary?.ocrSuccessCount ?? 0)} successful OCR runs across ${formatWholeNumber(summary?.ocrAttemptCount ?? 0)} attempts.`}
        />
        <AdminStatCard
          label="Complaint packs"
          value={formatWholeNumber(summary?.complaintPackGenerationCount ?? 0)}
          helperText="Generated complaint packs in the selected reporting slice."
        />
      </div>

      {!hasAnyData ? (
        <EmptyState
          title="No admin data for this filter set"
          description="Try widening the date range or removing the city filter to surface cases, uploads, and OCR activity."
        />
      ) : null}

      <div className="grid gap-6 xl:grid-cols-3">
        <BarListChart
          title="Cases by classification"
          description="A quick view of how the current case pool is distributed."
          items={casesByClassification
            .filter((item) => item.count > 0)
            .map((item) => ({
              id: item.classification,
              label: humanizeCode(item.classification),
              value: item.count,
              valueLabel: formatWholeNumber(item.count),
              description: `${formatWholeNumber(item.count)} case${item.count === 1 ? "" : "s"}`
            }))}
          emptyMessage="Classification counts will appear once matching cases exist."
        />

        <BarListChart
          title="Top merchants by risk"
          description="Highest dynamic merchant scores for the current filter window."
          items={topMerchantChartRows.map((merchant) => ({
            id: merchant.merchantId,
            label: merchant.merchantName,
            value: merchant.riskScore,
            valueLabel: merchant.riskScore.toFixed(1),
            description: `${formatWholeNumber(merchant.totalCases)} cases - ${formatWholeNumber(merchant.likelyCardSurchargeCases)} likely surcharges`,
            badgeLabel: humanizeCode(merchant.riskLabel),
            badgeTone: riskTone(merchant.riskLabel as RiskLabel)
          }))}
          accentClassName="bg-rose-500"
          emptyMessage="Merchant risk scores will appear once analyzed cases are available."
        />

        <BarListChart
          title="Top branches by risk"
          description="Branch hotspots help surface localized patterns inside larger merchants."
          items={topBranchChartRows.map((branch) => ({
            id: branch.branchId,
            label: branch.branchName,
            value: branch.riskScore,
            valueLabel: branch.riskScore.toFixed(1),
            description: `${branch.city}, ${branch.province} - ${formatWholeNumber(branch.totalCases)} cases`,
            badgeLabel: humanizeCode(branch.riskLabel),
            badgeTone: riskTone(branch.riskLabel as RiskLabel)
          }))}
          accentClassName="bg-teal-500"
          emptyMessage="Branch risk scores will appear once analyzed cases are available."
        />
      </div>

      <div className="grid gap-6">
        <TableShell
          title="Top merchants"
          description="Paginated merchant ranking for the active filters."
          footer={
            <TablePager
              pageIndex={merchantPageIndex}
              pageSize={TABLE_PAGE_SIZE}
              totalCount={merchantRows?.totalCount ?? 0}
              onPrevious={() => setMerchantPageIndex((current) => Math.max(0, current - 1))}
              onNext={() => setMerchantPageIndex((current) => current + 1)}
            />
          }
        >
          <>
            <table className="min-w-full text-left text-sm">
              <thead className="text-xs uppercase tracking-[0.18em] text-slate-500">
                <tr>
                  <th className="pb-3 pr-4 font-semibold">Merchant</th>
                  <th className="pb-3 pr-4 font-semibold">Category</th>
                  <th className="pb-3 pr-4 font-semibold">Risk</th>
                  <th className="pb-3 pr-4 font-semibold">Cases</th>
                  <th className="pb-3 font-semibold">Open</th>
                </tr>
              </thead>
              <tbody>
                {(merchantRows?.items ?? []).map((row) => (
                  <tr key={row.merchantId} className="border-t border-slate-100">
                    <td className="py-4 pr-4">
                      <div className="space-y-1">
                        <Link href={`/merchants/${row.merchantId}`} className="font-semibold text-slate-950 hover:text-amber-700">
                          {row.merchantName}
                        </Link>
                        <p className="text-xs text-slate-500">
                          {formatWholeNumber(row.likelyCardSurchargeCases)} likely card surcharge cases
                        </p>
                      </div>
                    </td>
                    <td className="py-4 pr-4 text-slate-600">{row.category ?? "Unspecified"}</td>
                    <td className="py-4 pr-4">
                      <div className="flex items-center gap-2">
                        <span className="font-semibold text-slate-950">{row.riskScore.toFixed(1)}</span>
                        <Badge tone={riskTone(row.riskLabel as RiskLabel)}>{humanizeCode(row.riskLabel)}</Badge>
                      </div>
                    </td>
                    <td className="py-4 pr-4 text-slate-600">{formatWholeNumber(row.totalCases)}</td>
                    <td className="py-4 text-slate-600">{formatWholeNumber(Math.max(row.totalCases - row.analyzedCases, 0))}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {(merchantRows?.items.length ?? 0) === 0 ? (
              <div className="rounded-3xl border border-dashed border-slate-200 px-4 py-8 text-sm text-slate-500">
                No merchant rows match the selected filters.
              </div>
            ) : null}
          </>
        </TableShell>

        <TableShell
          title="Top branches"
          description="Paginated branch ranking so the review team can narrow follow-up work."
          footer={
            <TablePager
              pageIndex={branchPageIndex}
              pageSize={TABLE_PAGE_SIZE}
              totalCount={branchRows?.totalCount ?? 0}
              onPrevious={() => setBranchPageIndex((current) => Math.max(0, current - 1))}
              onNext={() => setBranchPageIndex((current) => current + 1)}
            />
          }
        >
          <>
            <table className="min-w-full text-left text-sm">
              <thead className="text-xs uppercase tracking-[0.18em] text-slate-500">
                <tr>
                  <th className="pb-3 pr-4 font-semibold">Branch</th>
                  <th className="pb-3 pr-4 font-semibold">Merchant</th>
                  <th className="pb-3 pr-4 font-semibold">Location</th>
                  <th className="pb-3 pr-4 font-semibold">Risk</th>
                  <th className="pb-3 font-semibold">Cases</th>
                </tr>
              </thead>
              <tbody>
                {(branchRows?.items ?? []).map((row) => (
                  <tr key={row.branchId} className="border-t border-slate-100">
                    <td className="py-4 pr-4 font-semibold text-slate-950">{row.branchName}</td>
                    <td className="py-4 pr-4">
                      <Link href={`/merchants/${row.merchantId}`} className="text-slate-700 hover:text-amber-700">
                        {row.merchantName}
                      </Link>
                    </td>
                    <td className="py-4 pr-4 text-slate-600">
                      {row.city}, {row.province}
                    </td>
                    <td className="py-4 pr-4">
                      <div className="flex items-center gap-2">
                        <span className="font-semibold text-slate-950">{row.riskScore.toFixed(1)}</span>
                        <Badge tone={riskTone(row.riskLabel as RiskLabel)}>{humanizeCode(row.riskLabel)}</Badge>
                      </div>
                    </td>
                    <td className="py-4 text-slate-600">{formatWholeNumber(row.totalCases)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {(branchRows?.items.length ?? 0) === 0 ? (
              <div className="rounded-3xl border border-dashed border-slate-200 px-4 py-8 text-sm text-slate-500">
                No branch rows match the selected filters.
              </div>
            ) : null}
          </>
        </TableShell>

        <TableShell
          title="Recent uploads"
          description="Latest price and receipt uploads linked to the current reporting slice."
          footer={
            <TablePager
              pageIndex={uploadPageIndex}
              pageSize={TABLE_PAGE_SIZE}
              totalCount={uploadRows?.totalCount ?? 0}
              onPrevious={() => setUploadPageIndex((current) => Math.max(0, current - 1))}
              onNext={() => setUploadPageIndex((current) => current + 1)}
            />
          }
        >
          <>
            <table className="min-w-full text-left text-sm">
              <thead className="text-xs uppercase tracking-[0.18em] text-slate-500">
                <tr>
                  <th className="pb-3 pr-4 font-semibold">Upload</th>
                  <th className="pb-3 pr-4 font-semibold">Merchant</th>
                  <th className="pb-3 pr-4 font-semibold">File</th>
                  <th className="pb-3 pr-4 font-semibold">Uploaded by</th>
                  <th className="pb-3 font-semibold">Time</th>
                </tr>
              </thead>
              <tbody>
                {(uploadRows?.items ?? []).map((row) => (
                  <tr key={`${row.caseId}-${row.storagePath}-${row.uploadedUtc}`} className="border-t border-slate-100">
                    <td className="py-4 pr-4">
                      <div className="space-y-1">
                        <p className="font-semibold text-slate-950">{row.uploadKind}</p>
                        <Link href={`/cases/${row.caseId}`} className="text-xs text-slate-500 hover:text-amber-700">
                          Open case
                        </Link>
                      </div>
                    </td>
                    <td className="py-4 pr-4 text-slate-600">
                      <div>{row.merchantName}</div>
                      <div className="text-xs text-slate-500">
                        {row.branchName ? `${row.branchName} - ` : ""}
                        {[row.city, row.province].filter(Boolean).join(", ") || "Location not recorded"}
                      </div>
                    </td>
                    <td className="py-4 pr-4">
                      <a
                        href={buildUploadContentUrl(row.storagePath)}
                        target="_blank"
                        rel="noreferrer"
                        className="font-semibold text-slate-950 hover:text-amber-700"
                      >
                        {row.fileName}
                      </a>
                      <p className="text-xs text-slate-500">{humanizeCode(row.evidenceType)}</p>
                    </td>
                    <td className="py-4 pr-4 text-slate-600">{row.uploadedBy}</td>
                    <td className="py-4 text-slate-600">{formatDateTime(row.uploadedUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            {(uploadRows?.items.length ?? 0) === 0 ? (
              <div className="rounded-3xl border border-dashed border-slate-200 px-4 py-8 text-sm text-slate-500">
                No uploads match the selected filters.
              </div>
            ) : null}
          </>
        </TableShell>
      </div>
    </div>
  );
}
