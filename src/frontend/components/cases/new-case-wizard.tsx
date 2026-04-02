"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { formatCurrency, humanizeCode } from "@/lib/format";
import type {
  AnalysisClassification,
  BootstrapLookups,
  CaseAnalysis,
  CaseDetail,
  CaptureType,
  GeneratedComplaintPack,
  PaymentMethod,
  RunReceiptOcrResult
} from "@/lib/types";
import { createTextEvidenceFile, deriveEvidenceType, toDateTimeLocalInput, toUtcIsoString } from "@/lib/utils";
import {
  analysisSchema,
  caseDetailsSchema,
  flattenZodErrors,
  paymentEvidenceSchema,
  priceEvidenceSchema,
  validatePositiveMoney
} from "@/lib/validators";
import { SelectedFilePreview } from "@/components/evidence/file-preview";
import { ComplaintPackSubmissionGuide } from "@/components/complaint-packs/complaint-pack-submission-guide";
import { useSession } from "@/components/providers/session-provider";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ButtonLink } from "@/components/ui/button-link";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { CheckboxField, FieldShell, SelectInput, TextArea, TextInput } from "@/components/ui/field";
import { PageHeader } from "@/components/ui/page-header";
import { ErrorState, LoadingState } from "@/components/ui/state";

type CaptureFlow = "shelfPrice" | "quotedPrice" | "audioVideoQuote" | "receiptOnly";
type MerchantMode = "known" | "custom";

type DraftState = {
  captureFlow: CaptureFlow;
  merchantMode: MerchantMode;
  merchantId: string;
  branchId: string;
  customMerchantName: string;
  basketDescription: string;
  incidentAtLocal: string;
  currencyCode: string;
  customerReference: string;
  notes: string;
  quotedAmount: string;
  capturedAtLocal: string;
  merchantStatement: string;
  priceNotes: string;
  priceFile: File | null;
  amount: string;
  paymentMethod: PaymentMethod;
  paidAtLocal: string;
  paymentReference: string;
  merchantReference: string;
  cardLastFour: string;
  paymentNotes: string;
  receiptFile: File | null;
  parsedTotalAmount: string;
  receiptNumber: string;
  receiptMerchantName: string;
  receiptRawText: string;
  merchantSaidCardFee: boolean;
  cashbackPresent: boolean;
  deliveryOrServiceFeePresent: boolean;
  analysisEvidenceText: string;
};

const stepLabels = ["Capture type", "Store details", "Price evidence", "Payment evidence", "Review", "Analyze", "Complaint pack"];
const captureOptions: { id: CaptureFlow; title: string; description: string; backendType?: CaptureType; requiresMedia?: boolean }[] = [
  { id: "shelfPrice", title: "Shelf price", description: "A shelf ticket or in-store display before payment.", backendType: "ShelfPhoto" },
  { id: "quotedPrice", title: "Quoted price", description: "A written quote, message, document, or typed note.", backendType: "QuotationDocument" },
  { id: "audioVideoQuote", title: "Audio/video quote", description: "A spoken quote captured as audio or video evidence.", backendType: "ManualEntry", requiresMedia: true },
  { id: "receiptOnly", title: "Receipt only", description: "You only have the final receipt for now. Analysis stays blocked until quote evidence exists." }
];
const paymentMethodOptions: PaymentMethod[] = ["Cash", "DebitCard", "CreditCard", "BankTransfer", "Wallet"];
const supportedRasterMimeTypes = ["image/jpeg", "image/png", "image/webp"] as const;
const supportedDocumentMimeTypes = ["application/pdf", "text/plain"] as const;
const supportedAudioVideoMimeTypes = ["audio/mpeg", "audio/mp4", "audio/wav", "video/mp4", "video/webm"] as const;
const supportedRasterExtensions = [".jpg", ".jpeg", ".png", ".webp"] as const;
const supportedDocumentExtensions = [".pdf", ".txt"] as const;
const supportedAudioVideoExtensions = [".mp3", ".m4a", ".wav", ".mp4", ".webm"] as const;
const receiptFileAccept = [...supportedRasterMimeTypes, ...supportedDocumentMimeTypes].join(",");
const quoteEvidenceAccept = [...supportedRasterMimeTypes, ...supportedDocumentMimeTypes, ...supportedAudioVideoMimeTypes].join(",");
const audioVideoEvidenceAccept = supportedAudioVideoMimeTypes.join(",");
const customMerchantOptionValue = "__custom__";
const receiptFileHint = "Supported formats: JPG, PNG, WEBP, PDF, or TXT up to 20 MB.";
const quoteFileHint = "Supported formats: JPG, PNG, WEBP, PDF, MP3, M4A, WAV, MP4, WEBM, or TXT up to 20 MB.";
const audioVideoFileHint = "Supported formats: MP3, M4A, WAV, MP4, or WEBM up to 20 MB.";

function analysisTone(classification: string) {
  const value = classification as AnalysisClassification;

  if (value === "Match" || value === "LowerThanQuoted") {
    return "success";
  }

  if (value === "LikelyCardSurcharge" || value === "UnclearPositiveMismatch") {
    return "danger";
  }

  return "warning";
}

function isAllowedFile(file: File, allowedMimeTypes: readonly string[], allowedExtensions: readonly string[]) {
  const normalizedMimeType = file.type.trim().toLowerCase();
  if (normalizedMimeType && allowedMimeTypes.includes(normalizedMimeType)) {
    return true;
  }

  const normalizedName = file.name.trim().toLowerCase();
  return allowedExtensions.some((extension) => normalizedName.endsWith(extension));
}

function validateSelectedFile(
  file: File | null,
  allowedMimeTypes: readonly string[],
  allowedExtensions: readonly string[],
  message: string)
{
  if (!file) {
    return null;
  }

  return isAllowedFile(file, allowedMimeTypes, allowedExtensions) ? null : message;
}

export function NewCaseWizard() {
  const router = useRouter();
  const { preferences, session } = useSession();
  const [lookups, setLookups] = useState<BootstrapLookups | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [step, setStep] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [analyzing, setAnalyzing] = useState(false);
  const [generatingPack, setGeneratingPack] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [activityMessage, setActivityMessage] = useState<string | null>(null);
  const [createdCase, setCreatedCase] = useState<CaseDetail | null>(null);
  const [analysis, setAnalysis] = useState<CaseAnalysis | null>(null);
  const [ocrResult, setOcrResult] = useState<RunReceiptOcrResult | null>(null);
  const [generatedPack, setGeneratedPack] = useState<GeneratedComplaintPack | null>(null);
  const [draft, setDraft] = useState<DraftState>({
    captureFlow: "shelfPrice",
    merchantMode: "known",
    merchantId: "",
    branchId: "",
    customMerchantName: "",
    basketDescription: "",
    incidentAtLocal: toDateTimeLocalInput(new Date()),
    currencyCode: preferences.preferredCurrency,
    customerReference: "",
    notes: "",
    quotedAmount: "",
    capturedAtLocal: toDateTimeLocalInput(new Date()),
    merchantStatement: "",
    priceNotes: "",
    priceFile: null,
    amount: "",
    paymentMethod: "CreditCard",
    paidAtLocal: toDateTimeLocalInput(new Date()),
    paymentReference: "",
    merchantReference: "",
    cardLastFour: "",
    paymentNotes: "",
    receiptFile: null,
    parsedTotalAmount: "",
    receiptNumber: "",
    receiptMerchantName: "",
    receiptRawText: "",
    merchantSaidCardFee: false,
    cashbackPresent: false,
    deliveryOrServiceFeePresent: false,
    analysisEvidenceText: ""
  });

  useEffect(() => {
    let isCancelled = false;

    async function loadLookups() {
      setLoading(true);
      setError(null);

      try {
        const bootstrap = await api.getBootstrapLookups();
        if (!isCancelled) {
          setLookups(bootstrap);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setError(loadError instanceof Error ? loadError.message : "Unable to load merchants and branches.");
        }
      } finally {
        if (!isCancelled) {
          setLoading(false);
        }
      }
    }

    void loadLookups();
    return () => {
      isCancelled = true;
    };
  }, []);

  const selectedMerchant = draft.merchantMode === "known"
    ? lookups?.merchants.find((merchant) => merchant.id === draft.merchantId) ?? null
    : null;
  const availableBranches = selectedMerchant?.branches ?? [];
  const canAnalyze = createdCase?.latestQuotedAmount !== null && createdCase?.latestQuotedAmount !== undefined && createdCase?.latestPaidAmount !== null && createdCase?.latestPaidAmount !== undefined;
  const merchantDisplayName = draft.merchantMode === "custom"
    ? draft.customMerchantName.trim() || "Not entered"
    : selectedMerchant?.name ?? "Not selected";
  const branchDisplayName = draft.merchantMode === "known"
    ? availableBranches.find((branch) => branch.id === draft.branchId)?.name ?? "Not selected"
    : "Not applicable";

  function updateDraft<K extends keyof DraftState>(key: K, value: DraftState[K]) {
    setDraft((current) => ({ ...current, [key]: value }));
  }

  function setMerchantMode(mode: MerchantMode) {
    setErrors((current) => {
      const next = { ...current };
      delete next.merchantId;
      delete next.branchId;
      delete next.customMerchantName;
      return next;
    });

    setDraft((current) => mode === "known"
      ? { ...current, merchantMode: mode, customMerchantName: "" }
      : { ...current, merchantMode: mode, merchantId: "", branchId: "" });
  }

  function validateCurrentStep() {
    if (step === 0) {
      return {};
    }

    if (step === 1) {
      const parsed = caseDetailsSchema.safeParse({
        merchantMode: draft.merchantMode,
        merchantId: draft.merchantMode === "known" ? draft.merchantId : "",
        branchId: draft.merchantMode === "known" ? draft.branchId : "",
        customMerchantName: draft.customMerchantName,
        basketDescription: draft.basketDescription,
        incidentAtLocal: draft.incidentAtLocal,
        currencyCode: draft.currencyCode.toUpperCase(),
        customerReference: draft.customerReference,
        notes: draft.notes
      });

      return parsed.success ? {} : flattenZodErrors(parsed.error.issues);
    }

    if (step === 2) {
      const parsed = priceEvidenceSchema.safeParse({
        quotedAmount: draft.quotedAmount,
        capturedAtLocal: draft.capturedAtLocal,
        merchantStatement: draft.merchantStatement,
        notes: draft.priceNotes
      });

      const nextErrors = parsed.success ? {} : flattenZodErrors(parsed.error.issues);
      if (draft.captureFlow !== "receiptOnly") {
        const quotedAmountError = validatePositiveMoney(draft.quotedAmount, "Quoted amount");
        if (quotedAmountError) {
          nextErrors.quotedAmount = quotedAmountError;
        }

        const selectedCapture = captureOptions.find((option) => option.id === draft.captureFlow);
        const invalidPriceFileMessage = selectedCapture?.requiresMedia
          ? "Quote evidence must be MP3, M4A, WAV, MP4, or WEBM."
          : "Quote evidence must be JPG, PNG, WEBP, PDF, MP3, M4A, WAV, MP4, WEBM, or TXT.";
        const invalidPriceFile = validateSelectedFile(
          draft.priceFile,
          selectedCapture?.requiresMedia ? supportedAudioVideoMimeTypes : [...supportedRasterMimeTypes, ...supportedDocumentMimeTypes, ...supportedAudioVideoMimeTypes],
          selectedCapture?.requiresMedia ? supportedAudioVideoExtensions : [...supportedRasterExtensions, ...supportedDocumentExtensions, ...supportedAudioVideoExtensions],
          invalidPriceFileMessage);

        if (invalidPriceFile) {
          nextErrors.priceFile = invalidPriceFile;
        }

        if (selectedCapture?.requiresMedia && !draft.priceFile) {
          nextErrors.priceFile = "Upload an audio or video file for this capture type.";
        }
      }

      return nextErrors;
    }

    if (step === 3) {
      const parsed = paymentEvidenceSchema.safeParse({
        amount: draft.amount,
        paymentMethod: draft.paymentMethod,
        paidAtLocal: draft.paidAtLocal,
        paymentReference: draft.paymentReference,
        merchantReference: draft.merchantReference,
        cardLastFour: draft.cardLastFour,
        notes: draft.paymentNotes,
        parsedTotalAmount: draft.parsedTotalAmount,
        receiptNumber: draft.receiptNumber,
        merchantName: draft.receiptMerchantName,
        rawText: draft.receiptRawText
      });

      const nextErrors = parsed.success ? {} : flattenZodErrors(parsed.error.issues);
      const amountError = validatePositiveMoney(draft.amount, "Charged amount");
      if (amountError) {
        nextErrors.amount = amountError;
      }

      const invalidReceiptFile = validateSelectedFile(
        draft.receiptFile,
        [...supportedRasterMimeTypes, ...supportedDocumentMimeTypes],
        [...supportedRasterExtensions, ...supportedDocumentExtensions],
        "Receipt evidence must be JPG, PNG, WEBP, PDF, or TXT.");

      if (invalidReceiptFile) {
        nextErrors.receiptFile = invalidReceiptFile;
      }

      return nextErrors;
    }

    if (step === 5) {
      const parsed = analysisSchema.safeParse({
        merchantSaidCardFee: draft.merchantSaidCardFee,
        cashbackPresent: draft.cashbackPresent,
        deliveryOrServiceFeePresent: draft.deliveryOrServiceFeePresent,
        evidenceText: draft.analysisEvidenceText
      });

      return parsed.success ? {} : flattenZodErrors(parsed.error.issues);
    }

    return {};
  }

  async function handleSubmitCase() {
    if (!session) {
      return;
    }

    const nextErrors = validateCurrentStep();
    if (Object.keys(nextErrors).length > 0) {
      setErrors(nextErrors);
      return;
    }

    setSubmitting(true);
    setErrors({});
    setActivityMessage(null);

    try {
      const caseResult = await api.createCase({
        reportedByUserId: session.userId,
        merchantId: draft.merchantMode === "known" ? draft.merchantId : null,
        branchId: draft.merchantMode === "known" ? (draft.branchId || null) : null,
        customMerchantName: draft.merchantMode === "custom" ? (draft.customMerchantName.trim() || null) : null,
        basketDescription: draft.basketDescription,
        incidentAtUtc: toUtcIsoString(draft.incidentAtLocal),
        currencyCode: draft.currencyCode.toUpperCase(),
        customerReference: draft.customerReference || null,
        notes: draft.notes || null
      });

      if (draft.captureFlow !== "receiptOnly") {
        const priceFile =
          draft.priceFile ??
          createTextEvidenceFile(
            `${caseResult.caseNumber.toLowerCase()}-quote-note.txt`,
            `Quoted amount: ${draft.currencyCode.toUpperCase()} ${draft.quotedAmount}\nStatement: ${draft.merchantStatement}\nNotes: ${draft.priceNotes}`
          );

        const uploadedPrice = await api.uploadFile(priceFile, "price-evidence", caseResult.id);
        const selectedCapture = captureOptions.find((option) => option.id === draft.captureFlow);

        await api.createPriceCapture({
          caseId: caseResult.id,
          capturedByUserId: session.userId,
          captureType: selectedCapture?.backendType ?? "ManualEntry",
          evidenceType: deriveEvidenceType(priceFile),
          quotedAmount: Number(draft.quotedAmount),
          currencyCode: draft.currencyCode.toUpperCase(),
          fileName: uploadedPrice.fileName,
          evidenceStoragePath: uploadedPrice.storagePath,
          capturedAtUtc: toUtcIsoString(draft.capturedAtLocal),
          contentType: uploadedPrice.contentType,
          evidenceHash: uploadedPrice.contentHash,
          merchantStatement: draft.merchantStatement || null,
          notes: draft.priceNotes || null
        });
      }

      const paymentRecord = await api.createPaymentRecord({
        caseId: caseResult.id,
        recordedByUserId: session.userId,
        paymentMethod: draft.paymentMethod,
        amount: Number(draft.amount),
        currencyCode: draft.currencyCode.toUpperCase(),
        paidAtUtc: toUtcIsoString(draft.paidAtLocal),
        paymentReference: draft.paymentReference || null,
        merchantReference: draft.merchantReference || null,
        cardLastFour: draft.cardLastFour || null,
        notes: draft.paymentNotes || null
      });

      if (draft.receiptFile) {
        const uploadedReceipt = await api.uploadFile(draft.receiptFile, "receipt-evidence", caseResult.id);
        const receipt = await api.createReceiptRecord({
          caseId: caseResult.id,
          paymentRecordId: paymentRecord.id,
          uploadedByUserId: session.userId,
          evidenceType: deriveEvidenceType(draft.receiptFile),
          fileName: uploadedReceipt.fileName,
          contentType: uploadedReceipt.contentType,
          storagePath: uploadedReceipt.storagePath,
          uploadedAtUtc: new Date().toISOString(),
          currencyCode: draft.currencyCode.toUpperCase(),
          parsedTotalAmount: draft.parsedTotalAmount ? Number(draft.parsedTotalAmount) : null,
          receiptNumber: draft.receiptNumber || null,
          merchantName: draft.receiptMerchantName || null,
          rawText: draft.receiptRawText || null,
          fileHash: uploadedReceipt.contentHash
        });

        if (preferences.autoRunOcr) {
          try {
            const ocr = await api.runReceiptOcr(receipt.id);
            setOcrResult(ocr);
          } catch (ocrError) {
            setActivityMessage(ocrError instanceof Error ? `Case created, but OCR could not complete: ${ocrError.message}` : "Case created, but OCR could not complete.");
          }
        }
      }

      const refreshed = await api.getCase(caseResult.id);
      setCreatedCase(refreshed);

      if (preferences.autoAnalyzeCase && refreshed.latestQuotedAmount !== null && refreshed.latestQuotedAmount !== undefined && refreshed.latestPaidAmount !== null && refreshed.latestPaidAmount !== undefined) {
        try {
          const autoAnalysis = await api.analyzeCase(refreshed.id, {
            merchantSaidCardFee: draft.merchantSaidCardFee,
            cashbackPresent: draft.cashbackPresent,
            deliveryOrServiceFeePresent: draft.deliveryOrServiceFeePresent,
            evidenceText: draft.analysisEvidenceText || null
          });
          setAnalysis(autoAnalysis);
        } catch (analysisError) {
          setActivityMessage(analysisError instanceof Error ? `Case created, but analysis is still pending: ${analysisError.message}` : "Case created, but analysis is still pending.");
        }
      }

      setStep(5);
    } catch (submitError) {
      setActivityMessage(submitError instanceof Error ? submitError.message : "Unable to submit the case.");
    } finally {
      setSubmitting(false);
    }
  }

  async function handleRunAnalysis() {
    if (!createdCase) {
      return;
    }

    const nextErrors = validateCurrentStep();
    if (Object.keys(nextErrors).length > 0) {
      setErrors(nextErrors);
      return;
    }

    setAnalyzing(true);
    setActivityMessage(null);

    try {
      const result = await api.analyzeCase(createdCase.id, {
        merchantSaidCardFee: draft.merchantSaidCardFee,
        cashbackPresent: draft.cashbackPresent,
        deliveryOrServiceFeePresent: draft.deliveryOrServiceFeePresent,
        evidenceText: draft.analysisEvidenceText || null
      });

      setAnalysis(result);
      setCreatedCase(await api.getCase(createdCase.id));
    } catch (analyzeError) {
      setActivityMessage(analyzeError instanceof Error ? analyzeError.message : "Unable to analyze the case.");
    } finally {
      setAnalyzing(false);
    }
  }

  async function handleGeneratePack() {
    if (!createdCase) {
      return;
    }

    setGeneratingPack(true);
    setActivityMessage(null);

    try {
      const pack = await api.generateComplaintPack(createdCase.id);
      setGeneratedPack(pack);
      setCreatedCase(await api.getCase(createdCase.id));
    } catch (packError) {
      setActivityMessage(packError instanceof Error ? packError.message : "Unable to generate the complaint pack.");
    } finally {
      setGeneratingPack(false);
    }
  }

  const reviewItems = [
    ["Capture type", captureOptions.find((option) => option.id === draft.captureFlow)?.title ?? draft.captureFlow],
    ["Merchant", merchantDisplayName],
    ["Branch", branchDisplayName],
    ["Quoted amount", draft.captureFlow === "receiptOnly" ? "Not yet available" : formatCurrency(Number(draft.quotedAmount || 0), draft.currencyCode)],
    ["Charged amount", formatCurrency(Number(draft.amount || 0), draft.currencyCode)]
  ];

  if (loading) {
    return <LoadingState title="Preparing case wizard" message="Loading merchants, branches, and your workspace preferences." />;
  }

  if (error) {
    return <ErrorState message={error} actionLabel="Reload page" onAction={() => window.location.reload()} />;
  }

  return (
    <div className="space-y-6">
      <PageHeader eyebrow="Capture workflow" title="New case wizard" description="Move from quote evidence to analysis and complaint-pack generation using the live backend endpoints." />

      <Card className="overflow-x-auto">
        <div className="flex min-w-max gap-3">
          {stepLabels.map((label, index) => (
            <button
              key={label}
              type="button"
              onClick={() => setStep(index <= step || createdCase ? index : step)}
              className={`rounded-2xl px-4 py-3 text-left text-sm font-semibold transition ${index === step ? "bg-ink text-white" : index < step || createdCase ? "bg-slate-100 text-slate-800" : "bg-slate-50 text-slate-400"}`}
            >
              {index + 1}. {label}
            </button>
          ))}
        </div>
      </Card>

      {activityMessage ? <ErrorState title="Case activity update" message={activityMessage} /> : null}

      {step === 0 ? (
        <div className="grid gap-4 md:grid-cols-2">
          {captureOptions.map((option) => (
            <button
              key={option.id}
              type="button"
              onClick={() => updateDraft("captureFlow", option.id)}
              className={`rounded-[28px] border p-5 text-left transition ${draft.captureFlow === option.id ? "border-amber-500 bg-amber-50" : "border-white/70 bg-white/90 hover:border-slate-300"}`}
            >
              <p className="font-display text-xl font-semibold text-slate-950">{option.title}</p>
              <p className="mt-2 text-sm leading-6 text-slate-600">{option.description}</p>
            </button>
          ))}
        </div>
      ) : null}

      {step === 1 ? (
        <Card className="space-y-6">
          <div className="grid gap-3 sm:grid-cols-2">
            <button
              type="button"
              aria-pressed={draft.merchantMode === "known"}
              onClick={() => setMerchantMode("known")}
              className={`rounded-[28px] border p-5 text-left transition ${draft.merchantMode === "known" ? "border-amber-500 bg-amber-50" : "border-white/70 bg-white/90 hover:border-slate-300"}`}
            >
              <p className="font-display text-xl font-semibold text-slate-950">Choose known merchant</p>
              <p className="mt-2 text-sm leading-6 text-slate-600">Use one of the merchants already in the system and optionally tie the case to a branch.</p>
            </button>
            <button
              type="button"
              aria-pressed={draft.merchantMode === "custom"}
              onClick={() => setMerchantMode("custom")}
              className={`rounded-[28px] border p-5 text-left transition ${draft.merchantMode === "custom" ? "border-amber-500 bg-amber-50" : "border-white/70 bg-white/90 hover:border-slate-300"}`}
            >
              <p className="font-display text-xl font-semibold text-slate-950">Enter custom merchant</p>
              <p className="mt-2 text-sm leading-6 text-slate-600">Use this for informal shops, spaza stores, market stalls, or any merchant that is not listed yet.</p>
            </button>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            {draft.merchantMode === "known" ? (
              <>
                <FieldShell htmlFor="merchant" label="Merchant" error={errors.merchantId} required>
                  <SelectInput
                    id="merchant"
                    value={draft.merchantId}
                    onChange={(event) => {
                      const nextValue = event.target.value;
                      if (nextValue === customMerchantOptionValue) {
                        setMerchantMode("custom");
                        return;
                      }

                      updateDraft("merchantId", nextValue);
                      updateDraft("branchId", "");
                    }}
                  >
                    <option value="">Select a merchant</option>
                    {lookups?.merchants.map((merchant) => <option key={merchant.id} value={merchant.id}>{merchant.name}</option>)}
                    <option value={customMerchantOptionValue}>My merchant is not listed</option>
                  </SelectInput>
                  <button
                    type="button"
                    onClick={() => setMerchantMode("custom")}
                    className="mt-3 text-sm font-semibold text-amber-700 underline decoration-amber-300 underline-offset-4 transition hover:text-amber-800"
                  >
                    Can't find the merchant? Enter it manually instead.
                  </button>
                </FieldShell>
                <FieldShell htmlFor="branch" label="Branch" error={errors.branchId} hint="Optional if the dispute is tied to a specific branch or store location.">
                  <SelectInput id="branch" value={draft.branchId} onChange={(event) => updateDraft("branchId", event.target.value)} disabled={!draft.merchantId}>
                    <option value="">Select a branch</option>
                    {availableBranches.map((branch) => <option key={branch.id} value={branch.id}>{branch.name} - {branch.city}</option>)}
                  </SelectInput>
                </FieldShell>
              </>
            ) : (
              <>
                <FieldShell
                  htmlFor="custom-merchant"
                  label="Merchant name"
                  error={errors.customMerchantName}
                  hint="Use the name the customer would recognise, for example Corner Supermarket or Bheki's Spaza."
                  required
                >
                  <TextInput
                    id="custom-merchant"
                    value={draft.customMerchantName}
                    onChange={(event) => updateDraft("customMerchantName", event.target.value)}
                    placeholder="Enter the merchant name"
                  />
                </FieldShell>
                <div className="space-y-3 rounded-[28px] border border-dashed border-slate-300 bg-slate-50 px-5 py-4 text-sm leading-6 text-slate-600">
                  <p>Custom merchant cases start without a saved branch. You can still continue with price, payment, receipt, analysis, and complaint-pack generation immediately.</p>
                  <button
                    type="button"
                    onClick={() => setMerchantMode("known")}
                    className="text-sm font-semibold text-slate-700 underline decoration-slate-300 underline-offset-4 transition hover:text-slate-900"
                  >
                    Choose from the existing merchant list instead.
                  </button>
                </div>
              </>
            )}

            <FieldShell htmlFor="basket" label="Basket or item description" error={errors.basketDescription} required>
              <TextInput id="basket" value={draft.basketDescription} onChange={(event) => updateDraft("basketDescription", event.target.value)} />
            </FieldShell>
            <FieldShell htmlFor="incident" label="Incident time" error={errors.incidentAtLocal} required>
              <TextInput id="incident" type="datetime-local" value={draft.incidentAtLocal} onChange={(event) => updateDraft("incidentAtLocal", event.target.value)} />
            </FieldShell>
            <FieldShell htmlFor="currency" label="Currency code" error={errors.currencyCode} required>
              <TextInput id="currency" value={draft.currencyCode} onChange={(event) => updateDraft("currencyCode", event.target.value.toUpperCase())} />
            </FieldShell>
            <FieldShell htmlFor="reference" label="Customer reference" error={errors.customerReference}>
              <TextInput id="reference" value={draft.customerReference} onChange={(event) => updateDraft("customerReference", event.target.value)} />
            </FieldShell>
            <div className="md:col-span-2">
              <FieldShell htmlFor="notes" label="Case notes" error={errors.notes}>
                <TextArea id="notes" value={draft.notes} onChange={(event) => updateDraft("notes", event.target.value)} />
              </FieldShell>
            </div>
          </div>
        </Card>
      ) : null}

      {step === 2 ? (
        <div className="space-y-4">
          {draft.captureFlow === "receiptOnly" ? <Card><CardTitle>Receipt-only case</CardTitle><CardDescription>Skip quoted-price evidence for now. You can still record the final charge and add the missing quote evidence later from the case page.</CardDescription></Card> : null}
          {draft.captureFlow !== "receiptOnly" ? <Card className="grid gap-4 md:grid-cols-2"><FieldShell htmlFor="quoted-amount" label="Quoted or displayed amount" error={errors.quotedAmount} required><TextInput id="quoted-amount" inputMode="decimal" value={draft.quotedAmount} onChange={(event) => updateDraft("quotedAmount", event.target.value)} /></FieldShell><FieldShell htmlFor="captured-at" label="Captured at" error={errors.capturedAtLocal} required><TextInput id="captured-at" type="datetime-local" value={draft.capturedAtLocal} onChange={(event) => updateDraft("capturedAtLocal", event.target.value)} /></FieldShell><div className="md:col-span-2"><FieldShell htmlFor="price-file" label="Upload quote evidence" error={errors.priceFile} hint={draft.captureFlow === "audioVideoQuote" ? audioVideoFileHint : `${quoteFileHint} If you skip file upload, the app will attach a text evidence note instead.`}><TextInput id="price-file" type="file" accept={draft.captureFlow === "audioVideoQuote" ? audioVideoEvidenceAccept : quoteEvidenceAccept} onChange={(event) => updateDraft("priceFile", event.target.files?.[0] ?? null)} /></FieldShell></div><FieldShell htmlFor="merchant-statement" label="Merchant statement"><TextArea id="merchant-statement" value={draft.merchantStatement} onChange={(event) => updateDraft("merchantStatement", event.target.value)} /></FieldShell><FieldShell htmlFor="price-notes" label="Additional notes"><TextArea id="price-notes" value={draft.priceNotes} onChange={(event) => updateDraft("priceNotes", event.target.value)} /></FieldShell></Card> : null}
          <SelectedFilePreview file={draft.priceFile} title="Quoted price evidence" />
        </div>
      ) : null}

      {step === 3 ? (
        <div className="space-y-4">
          <Card className="grid gap-4 md:grid-cols-2">
            <FieldShell htmlFor="amount" label="Final charged amount" error={errors.amount} required><TextInput id="amount" inputMode="decimal" value={draft.amount} onChange={(event) => updateDraft("amount", event.target.value)} /></FieldShell>
            <FieldShell htmlFor="payment-method" label="Payment method" error={errors.paymentMethod} required><SelectInput id="payment-method" value={draft.paymentMethod} onChange={(event) => updateDraft("paymentMethod", event.target.value as PaymentMethod)}>{paymentMethodOptions.map((method) => <option key={method} value={method}>{humanizeCode(method)}</option>)}</SelectInput></FieldShell>
            <FieldShell htmlFor="paid-at" label="Paid at" error={errors.paidAtLocal} required><TextInput id="paid-at" type="datetime-local" value={draft.paidAtLocal} onChange={(event) => updateDraft("paidAtLocal", event.target.value)} /></FieldShell>
            <FieldShell htmlFor="card-last-four" label="Card last four" error={errors.cardLastFour}><TextInput id="card-last-four" maxLength={4} value={draft.cardLastFour} onChange={(event) => updateDraft("cardLastFour", event.target.value)} /></FieldShell>
            <FieldShell htmlFor="payment-reference" label="Payment reference"><TextInput id="payment-reference" value={draft.paymentReference} onChange={(event) => updateDraft("paymentReference", event.target.value)} /></FieldShell>
            <FieldShell htmlFor="merchant-reference" label="Merchant reference"><TextInput id="merchant-reference" value={draft.merchantReference} onChange={(event) => updateDraft("merchantReference", event.target.value)} /></FieldShell>
            <div className="md:col-span-2"><FieldShell htmlFor="receipt-file" label="Upload receipt evidence" error={errors.receiptFile} hint={`Optional, but strongly recommended for OCR and complaint-pack strength. ${receiptFileHint}`}><TextInput id="receipt-file" type="file" accept={receiptFileAccept} onChange={(event) => updateDraft("receiptFile", event.target.files?.[0] ?? null)} /></FieldShell></div>
            <FieldShell htmlFor="payment-notes" label="Payment notes"><TextArea id="payment-notes" value={draft.paymentNotes} onChange={(event) => updateDraft("paymentNotes", event.target.value)} /></FieldShell>
            <FieldShell htmlFor="receipt-raw" label="Receipt text notes"><TextArea id="receipt-raw" value={draft.receiptRawText} onChange={(event) => updateDraft("receiptRawText", event.target.value)} /></FieldShell>
          </Card>
          <SelectedFilePreview file={draft.receiptFile} title="Receipt evidence" />
        </div>
      ) : null}

      {step === 4 ? (
        <Card className="space-y-4">
          <CardTitle>Review and submit</CardTitle>
          <div className="grid gap-3 md:grid-cols-2">
            {reviewItems.map(([label, value]) => <div key={label} className="rounded-2xl bg-slate-50 p-4"><p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">{label}</p><p className="mt-2 text-sm font-semibold text-slate-950">{value}</p></div>)}
          </div>
          <Button onClick={handleSubmitCase} busy={submitting}>Create case and upload evidence</Button>
        </Card>
      ) : null}

      {step === 5 ? (
        <div className="space-y-4">
          <Card className="space-y-4">
            <CardTitle>Analyze result</CardTitle>
            <div className="grid gap-3 md:grid-cols-3">
              <CheckboxField id="fee-flag" label="Merchant said card fee" checked={draft.merchantSaidCardFee} onChange={(checked) => updateDraft("merchantSaidCardFee", checked)} />
              <CheckboxField id="cashback-flag" label="Cashback present" checked={draft.cashbackPresent} onChange={(checked) => updateDraft("cashbackPresent", checked)} />
              <CheckboxField id="delivery-flag" label="Delivery or service fee" checked={draft.deliveryOrServiceFeePresent} onChange={(checked) => updateDraft("deliveryOrServiceFeePresent", checked)} />
            </div>
            <FieldShell htmlFor="analysis-text" label="Evidence text for analysis" error={errors.evidenceText}><TextArea id="analysis-text" value={draft.analysisEvidenceText} onChange={(event) => updateDraft("analysisEvidenceText", event.target.value)} /></FieldShell>
            <div className="flex flex-wrap gap-3">
              <Button onClick={handleRunAnalysis} busy={analyzing} disabled={!canAnalyze}>Run analysis</Button>
              {createdCase ? <ButtonLink href={`/cases/${createdCase.id}`} variant="secondary">Open case details</ButtonLink> : null}
            </div>
            {!canAnalyze ? <CardDescription>Analysis becomes available once both the quoted amount and the charged amount are recorded.</CardDescription> : null}
          </Card>
          {analysis ? <Card className="space-y-4"><div className="flex items-center justify-between gap-3"><CardTitle>Latest analysis</CardTitle><Badge tone={analysisTone(analysis.classification)}>{humanizeCode(analysis.classification)}</Badge></div><div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4"><div className="rounded-2xl bg-slate-50 p-4"><p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Quoted</p><p className="mt-2 text-sm font-semibold text-slate-950">{formatCurrency(analysis.quotedAmount, draft.currencyCode)}</p></div><div className="rounded-2xl bg-slate-50 p-4"><p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Charged</p><p className="mt-2 text-sm font-semibold text-slate-950">{formatCurrency(analysis.chargedAmount, draft.currencyCode)}</p></div><div className="rounded-2xl bg-slate-50 p-4"><p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Difference</p><p className="mt-2 text-sm font-semibold text-slate-950">{formatCurrency(analysis.difference, draft.currencyCode)}</p></div><div className="rounded-2xl bg-slate-50 p-4"><p className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">Confidence</p><p className="mt-2 text-sm font-semibold text-slate-950">{Math.round(analysis.confidence * 100)}%</p></div></div><p className="text-sm leading-6 text-slate-700">{analysis.explanation}</p></Card> : null}
          {ocrResult ? <Card className="space-y-3"><CardTitle>Receipt OCR</CardTitle><CardDescription>Provider: {ocrResult.providerName} - Confidence: {Math.round(ocrResult.confidence * 100)}%</CardDescription><p className="text-sm leading-6 text-slate-700">{ocrResult.rawText || "OCR completed without extracted receipt text."}</p></Card> : null}
        </div>
      ) : null}

      {step === 6 ? (
        <Card className="space-y-4">
          <CardTitle>Generate complaint pack</CardTitle>
          <CardDescription>The pack generator stays factual and will call out weak evidence when the case record is incomplete.</CardDescription>
          <div className="flex flex-wrap gap-3">
            <Button onClick={handleGeneratePack} busy={generatingPack} disabled={!canAnalyze}>Generate complaint pack</Button>
            {generatedPack ? <Button variant="secondary" onClick={() => api.downloadComplaintPack(generatedPack.id, generatedPack.fileName)}>Download PDF</Button> : null}
          </div>
          {!canAnalyze ? <CardDescription>Complaint pack generation is blocked until both quoted and charged amounts are available.</CardDescription> : null}
          {generatedPack ? <Card className="space-y-3 border-slate-200"><Badge tone="info">{generatedPack.fileName}</Badge><p className="text-sm leading-6 text-slate-700">{generatedPack.summary}</p><p className="text-sm text-slate-600">Evidence strength: {generatedPack.jsonSummary.evidenceAssessment.strength}</p></Card> : null}
          {generatedPack ? <ComplaintPackSubmissionGuide guidance={generatedPack.jsonSummary.submissionGuidance} /> : null}
        </Card>
      ) : null}

      <div className="flex flex-wrap gap-3">
        <Button variant="secondary" onClick={() => setStep((current) => Math.max(0, current - 1))} disabled={step === 0 || submitting || analyzing || generatingPack}>Back</Button>
        {step < 4 ? <Button onClick={() => { const nextErrors = validateCurrentStep(); setErrors(nextErrors); if (Object.keys(nextErrors).length === 0) { setStep((current) => current + 1); } }}>Continue</Button> : null}
        {step === 5 ? <Button onClick={() => setStep(6)}>Continue to complaint pack</Button> : null}
        {step === 6 && createdCase ? <Button variant="secondary" onClick={() => router.push(`/cases/${createdCase.id}`)}>Go to case details</Button> : null}
      </div>
    </div>
  );
}
