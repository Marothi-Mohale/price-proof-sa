"use client";

import { useEffect, useState } from "react";
import { api, buildUploadContentUrl } from "@/lib/api";
import { formatCurrency, formatDateTime, humanizeCode } from "@/lib/format";
import type { CaseAnalysis, CaseDetail } from "@/lib/types";
import { Badge } from "@/components/ui/badge";
import { Button, buttonClasses } from "@/components/ui/button";
import { ButtonLink } from "@/components/ui/button-link";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { CheckboxField, FieldShell, TextArea } from "@/components/ui/field";
import { PageHeader } from "@/components/ui/page-header";
import { ErrorState, LoadingState } from "@/components/ui/state";

export function CaseDetailScreen({ caseId }: { caseId: string }) {
  const [caseDetail, setCaseDetail] = useState<CaseDetail | null>(null);
  const [analysis, setAnalysis] = useState<CaseAnalysis | null>(null);
  const [loading, setLoading] = useState(true);
  const [working, setWorking] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [flags, setFlags] = useState({
    merchantSaidCardFee: false,
    cashbackPresent: false,
    deliveryOrServiceFeePresent: false,
    evidenceText: ""
  });

  useEffect(() => {
    let isCancelled = false;

    async function loadCase() {
      setLoading(true);
      setError(null);

      try {
        const result = await api.getCase(caseId);
        if (!isCancelled) {
          setCaseDetail(result);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setError(loadError instanceof Error ? loadError.message : "Unable to load the case.");
        }
      } finally {
        if (!isCancelled) {
          setLoading(false);
        }
      }
    }

    void loadCase();
    return () => {
      isCancelled = true;
    };
  }, [caseId]);

  async function refreshCase() {
    const refreshed = await api.getCase(caseId);
    setCaseDetail(refreshed);
  }

  async function runAnalysis() {
    setWorking(true);
    setError(null);

    try {
      const result = await api.analyzeCase(caseId, flags);
      setAnalysis(result);
      await refreshCase();
    } catch (analysisError) {
      setError(analysisError instanceof Error ? analysisError.message : "Unable to analyze the case.");
    } finally {
      setWorking(false);
    }
  }

  async function generateComplaintPack() {
    setWorking(true);
    setError(null);

    try {
      await api.generateComplaintPack(caseId);
      await refreshCase();
    } catch (packError) {
      setError(packError instanceof Error ? packError.message : "Unable to generate the complaint pack.");
    } finally {
      setWorking(false);
    }
  }

  async function runReceiptOcr(receiptId: string) {
    setWorking(true);
    setError(null);

    try {
      await api.runReceiptOcr(receiptId);
      await refreshCase();
    } catch (ocrError) {
      setError(ocrError instanceof Error ? ocrError.message : "Unable to run OCR for this receipt.");
    } finally {
      setWorking(false);
    }
  }

  if (loading) {
    return <LoadingState title="Loading case" message="Pulling the latest evidence, payments, and complaint packs." />;
  }

  if (error && !caseDetail) {
    return <ErrorState message={error} actionLabel="Try again" onAction={() => window.location.reload()} />;
  }

  if (!caseDetail) {
    return <ErrorState message="The requested case could not be found." />;
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={caseDetail.caseNumber}
        title={caseDetail.merchant.name}
        description={`${caseDetail.branch?.name ? `${caseDetail.branch.name} - ` : ""}${caseDetail.basketDescription}`}
        actions={
          <>
            <Button variant="secondary" onClick={refreshCase}>Refresh</Button>
            <ButtonLink href={`/merchants/${caseDetail.merchant.id}`} variant="secondary">Merchant history</ButtonLink>
          </>
        }
      />

      {error ? <ErrorState title="Case action update" message={error} /> : null}

      <div className="grid gap-4 md:grid-cols-4">
        <Card><p className="text-sm text-slate-500">Status</p><p className="mt-3 font-display text-2xl font-semibold text-slate-950">{humanizeCode(caseDetail.status)}</p></Card>
        <Card><p className="text-sm text-slate-500">Classification</p><p className="mt-3 font-display text-2xl font-semibold text-slate-950">{humanizeCode(caseDetail.classification)}</p></Card>
        <Card><p className="text-sm text-slate-500">Quoted</p><p className="mt-3 font-display text-2xl font-semibold text-slate-950">{formatCurrency(caseDetail.latestQuotedAmount, caseDetail.currencyCode)}</p></Card>
        <Card><p className="text-sm text-slate-500">Charged</p><p className="mt-3 font-display text-2xl font-semibold text-slate-950">{formatCurrency(caseDetail.latestPaidAmount, caseDetail.currencyCode)}</p></Card>
      </div>

      <Card className="space-y-4">
        <CardTitle>Analyze and generate complaint pack</CardTitle>
        <div className="grid gap-3 md:grid-cols-3">
          <CheckboxField id="detail-fee" label="Merchant said card fee" checked={flags.merchantSaidCardFee} onChange={(checked) => setFlags((current) => ({ ...current, merchantSaidCardFee: checked }))} />
          <CheckboxField id="detail-cashback" label="Cashback present" checked={flags.cashbackPresent} onChange={(checked) => setFlags((current) => ({ ...current, cashbackPresent: checked }))} />
          <CheckboxField id="detail-service" label="Delivery or service fee" checked={flags.deliveryOrServiceFeePresent} onChange={(checked) => setFlags((current) => ({ ...current, deliveryOrServiceFeePresent: checked }))} />
        </div>
        <FieldShell htmlFor="analysis-evidence-text" label="Evidence text for analysis">
          <TextArea id="analysis-evidence-text" value={flags.evidenceText} onChange={(event) => setFlags((current) => ({ ...current, evidenceText: event.target.value }))} />
        </FieldShell>
        <div className="flex flex-wrap gap-3">
          <Button onClick={runAnalysis} busy={working}>Run analysis</Button>
          <Button variant="secondary" onClick={generateComplaintPack} busy={working}>Generate complaint pack</Button>
        </div>
        {analysis ? <div className="rounded-3xl bg-slate-50 p-4"><div className="flex items-center justify-between gap-3"><CardTitle>{humanizeCode(analysis.classification)}</CardTitle><Badge tone="info">{Math.round(analysis.confidence * 100)}% confidence</Badge></div><p className="mt-3 text-sm leading-6 text-slate-700">{analysis.explanation}</p></div> : null}
      </Card>

      <Card className="space-y-4">
        <CardTitle>Price evidence</CardTitle>
        <div className="grid gap-4">
          {caseDetail.priceCaptures.map((capture) => (
            <div key={capture.id} className="rounded-3xl border border-slate-200 p-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <p className="text-sm font-semibold text-slate-950">{capture.fileName}</p>
                  <p className="text-sm text-slate-600">{humanizeCode(capture.captureType)} - {formatDateTime(capture.capturedAtUtc)}</p>
                </div>
                <Badge tone="info">{formatCurrency(capture.quotedAmount, capture.currencyCode)}</Badge>
              </div>
              <div className="mt-3 flex flex-wrap gap-3">
                <a href={buildUploadContentUrl(capture.evidenceStoragePath)} target="_blank" rel="noreferrer" className={buttonClasses("secondary")}>Open evidence</a>
              </div>
            </div>
          ))}
        </div>
      </Card>

      <Card className="space-y-4">
        <CardTitle>Payment evidence</CardTitle>
        <div className="grid gap-4">
          {caseDetail.paymentRecords.map((record) => (
            <div key={record.id} className="rounded-3xl border border-slate-200 p-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <p className="text-sm font-semibold text-slate-950">{humanizeCode(record.paymentMethod)}</p>
                  <p className="text-sm text-slate-600">{formatDateTime(record.paidAtUtc)}</p>
                </div>
                <Badge tone="warning">{formatCurrency(record.amount, record.currencyCode)}</Badge>
              </div>
              {record.receipt ? <div className="mt-4 rounded-2xl bg-slate-50 p-4"><p className="text-sm font-semibold text-slate-950">{record.receipt.fileName}</p><p className="mt-1 text-sm text-slate-600">{record.receipt.merchantName ?? "Receipt uploaded"} - {formatCurrency(record.receipt.parsedTotalAmount, record.receipt.currencyCode)}</p><div className="mt-3 flex flex-wrap gap-3"><a href={buildUploadContentUrl(record.receipt.storagePath)} target="_blank" rel="noreferrer" className={buttonClasses("secondary")}>Open receipt</a><Button variant="secondary" onClick={() => runReceiptOcr(record.receipt!.id)} busy={working}>Run OCR</Button></div></div> : null}
            </div>
          ))}
        </div>
      </Card>

      <Card className="space-y-4">
        <CardTitle>Complaint packs</CardTitle>
        <div className="grid gap-4">
          {caseDetail.complaintPacks.map((pack) => (
            <div key={pack.id} className="rounded-3xl border border-slate-200 p-4">
              <p className="text-sm font-semibold text-slate-950">{pack.fileName}</p>
              <p className="mt-1 text-sm text-slate-600">{pack.summary}</p>
              <div className="mt-3 flex flex-wrap gap-3">
                <Button variant="secondary" onClick={() => api.downloadComplaintPack(pack.id, pack.fileName)}>Download PDF</Button>
              </div>
            </div>
          ))}
        </div>
      </Card>

      <Card className="space-y-4">
        <CardTitle>Audit timeline</CardTitle>
        <div className="grid gap-3">
          {caseDetail.auditLogs.map((log) => (
            <div key={log.id} className="rounded-2xl bg-slate-50 p-4">
              <p className="text-sm font-semibold text-slate-950">{log.action}</p>
              <p className="text-sm text-slate-600">{formatDateTime(log.occurredAtUtc)}</p>
            </div>
          ))}
        </div>
      </Card>
    </div>
  );
}
