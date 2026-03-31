"use client";

import { useEffect, useMemo, useState, useTransition } from "react";
import clsx from "clsx";
import { api } from "@/lib/api";
import { formatDate, formatMoney } from "@/lib/format";
import { compressImageFile } from "@/lib/image-compression";
import type { AuthSession, CaseDetail, CaseSummary, CurrentUser, MerchantHistory } from "@/lib/types";
import {
  EmptyState,
  FileField,
  InfoTile,
  InputField,
  MetricChip,
  SelectField,
  StatusBanner,
  StatusPill,
  TextAreaField,
  TimelineCard,
  ToggleField
} from "@/components/dashboard-primitives";

const SESSION_STORAGE_KEY = "priceproof-sa-session";

const mediaModes = [
  { value: "ShelfPricePhoto", label: "Shelf photo" },
  { value: "AudioQuote", label: "Audio quote" },
  { value: "VideoQuote", label: "Video quote" }
] as const;

const paymentModes = [
  { value: "ManualEntry", label: "Manual amount" },
  { value: "BankNotification", label: "Bank notification" }
] as const;

export function PriceProofDashboard() {
  const [session, setSession] = useState<AuthSession | null>(null);
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null);
  const [cases, setCases] = useState<CaseSummary[]>([]);
  const [selectedCaseId, setSelectedCaseId] = useState<string | null>(null);
  const [selectedCase, setSelectedCase] = useState<CaseDetail | null>(null);
  const [merchantHistory, setMerchantHistory] = useState<MerchantHistory | null>(null);
  const [authMode, setAuthMode] = useState<"sign-in" | "sign-up">("sign-in");
  const [email, setEmail] = useState("demo.user@priceproof.local");
  const [displayName, setDisplayName] = useState("Demo User");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [qrStubMessage, setQrStubMessage] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  const [createCaseForm, setCreateCaseForm] = useState({
    merchantName: "",
    merchantCategory: "",
    branchName: "",
    branchAddress: "",
    branchCity: "",
    branchProvince: "",
    basketDescription: ""
  });

  const [manualPriceForm, setManualPriceForm] = useState({ amount: "", quoteText: "", notes: "" });
  const [mediaPriceForm, setMediaPriceForm] = useState({ mode: "ShelfPricePhoto", amount: "", quoteText: "", notes: "", file: null as File | null });
  const [manualPaymentForm, setManualPaymentForm] = useState({
    mode: "ManualEntry",
    amount: "",
    isCardPayment: true,
    note: "",
    bankNotificationText: ""
  });
  const [receiptPaymentForm, setReceiptPaymentForm] = useState({
    enteredAmount: "",
    isCardPayment: true,
    note: "",
    file: null as File | null
  });

  const stats = useMemo(() => ({
    total: cases.length,
    flagged: cases.filter((item) => item.likelyUnlawfulCardSurcharge).length,
    ready: cases.filter((item) => item.status === "ReadyForComplaint").length
  }), [cases]);

  useEffect(() => {
    const storedSession = window.localStorage.getItem(SESSION_STORAGE_KEY);
    if (storedSession) {
      setSession(JSON.parse(storedSession) as AuthSession);
    }
  }, []);

  useEffect(() => {
    if (!session) {
      return;
    }

    startTransition(() => {
      void hydrateDashboard(session);
    });
  }, [session]);

  useEffect(() => {
    if (!session || !selectedCaseId) {
      return;
    }

    startTransition(() => {
      void refreshSelectedCase(session.sessionToken, selectedCaseId);
    });
  }, [selectedCaseId, session]);

  useEffect(() => {
    if (!session || !selectedCase?.merchantId) {
      setMerchantHistory(null);
      return;
    }

    startTransition(() => {
      void fetchMerchantHistory(session.sessionToken, selectedCase.merchantId);
    });
  }, [selectedCase?.merchantId, session]);

  async function hydrateDashboard(activeSession: AuthSession) {
    try {
      setError(null);
      const [user, nextCases] = await Promise.all([
        api.getCurrentUser(activeSession.sessionToken),
        api.listCases(activeSession.sessionToken)
      ]);

      setCurrentUser(user);
      setCases(nextCases);
      if (nextCases.length > 0 && !selectedCaseId) {
        setSelectedCaseId(nextCases[0].id);
      }
    } catch (nextError) {
      showError(nextError);
    }
  }

  async function refreshSelectedCase(token: string, caseId: string) {
    try {
      const detail = await api.getCase(token, caseId);
      setSelectedCase(detail);
      mergeCaseSummary(detail);
    } catch (nextError) {
      showError(nextError);
    }
  }

  async function fetchMerchantHistory(token: string, merchantId: string) {
    try {
      const history = await api.getMerchantHistory(token, merchantId);
      setMerchantHistory(history);
    } catch (nextError) {
      showError(nextError);
    }
  }

  function persistSession(nextSession: AuthSession | null) {
    setSession(nextSession);
    if (nextSession) {
      window.localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(nextSession));
    } else {
      window.localStorage.removeItem(SESSION_STORAGE_KEY);
      setCurrentUser(null);
      setCases([]);
      setSelectedCaseId(null);
      setSelectedCase(null);
      setMerchantHistory(null);
    }
  }

  function mergeCaseSummary(detail: CaseDetail) {
    setCases((current) => {
      const summary: CaseSummary = {
        id: detail.id,
        merchantName: detail.merchantName,
        basketDescription: detail.basketDescription,
        status: detail.status,
        quotedAmount: detail.quotedAmount,
        chargedAmount: detail.chargedAmount,
        differenceAmount: detail.differenceAmount,
        classification: detail.classification,
        likelyUnlawfulCardSurcharge: detail.likelyUnlawfulCardSurcharge,
        updatedAtUtc: detail.updatedAtUtc
      };

      const index = current.findIndex((item) => item.id === detail.id);
      if (index < 0) {
        return [summary, ...current];
      }

      const next = [...current];
      next[index] = summary;
      return next;
    });
  }

  function showError(nextError: unknown) {
    setError(nextError instanceof Error ? nextError.message : "Something went wrong.");
  }

  async function handleAuthenticate() {
    try {
      setError(null);
      const authSession =
        authMode === "sign-up"
          ? await api.signUp({ email, displayName })
          : await api.signIn({ email });

      persistSession(authSession);
      setMessage(`Signed in as ${authSession.displayName}.`);
    } catch (nextError) {
      showError(nextError);
    }
  }

  async function handleCreateCase() {
    if (!session) {
      return;
    }

    try {
      const detail = await api.createCase(session.sessionToken, {
        merchantName: createCaseForm.merchantName,
        merchantCategory: createCaseForm.merchantCategory || undefined,
        branchName: createCaseForm.branchName || undefined,
        branchAddress: createCaseForm.branchAddress || undefined,
        branchCity: createCaseForm.branchCity || undefined,
        branchProvince: createCaseForm.branchProvince || undefined,
        basketDescription: createCaseForm.basketDescription
      });

      setSelectedCase(detail);
      setSelectedCaseId(detail.id);
      mergeCaseSummary(detail);
      setMessage("New case created.");
      setCreateCaseForm({
        merchantName: "",
        merchantCategory: "",
        branchName: "",
        branchAddress: "",
        branchCity: "",
        branchProvince: "",
        basketDescription: ""
      });
    } catch (nextError) {
      showError(nextError);
    }
  }

  async function handleManualPriceCapture() {
    if (!session || !selectedCaseId) {
      return;
    }

    try {
      const detail = await api.addManualPriceCapture(session.sessionToken, selectedCaseId, {
        amount: Number(manualPriceForm.amount),
        quoteText: manualPriceForm.quoteText || undefined,
        notes: manualPriceForm.notes || undefined
      });

      setSelectedCase(detail);
      mergeCaseSummary(detail);
      setManualPriceForm({ amount: "", quoteText: "", notes: "" });
      setMessage("Quoted price saved.");
    } catch (nextError) {
      showError(nextError);
    }
  }

  async function handleMediaPriceCapture() {
    if (!session || !selectedCaseId || !mediaPriceForm.file) {
      return;
    }

    try {
      const file = await compressImageFile(mediaPriceForm.file);
      const detail = await api.addMediaPriceCapture(session.sessionToken, selectedCaseId, {
        mode: mediaPriceForm.mode,
        amount: mediaPriceForm.amount ? Number(mediaPriceForm.amount) : undefined,
        quoteText: mediaPriceForm.quoteText || undefined,
        notes: mediaPriceForm.notes || undefined,
        file
      });

      setSelectedCase(detail);
      mergeCaseSummary(detail);
      setMediaPriceForm({ mode: "ShelfPricePhoto", amount: "", quoteText: "", notes: "", file: null });
      setMessage("Pre-payment evidence uploaded.");
    } catch (nextError) {
      showError(nextError);
    }
  }

  async function handleManualPayment() {
    if (!session || !selectedCaseId) {
      return;
    }

    try {
      const detail = await api.addManualPayment(session.sessionToken, selectedCaseId, {
        amount: Number(manualPaymentForm.amount),
        mode: manualPaymentForm.mode,
        isCardPayment: manualPaymentForm.isCardPayment,
        note: manualPaymentForm.note || undefined,
        bankNotificationText: manualPaymentForm.bankNotificationText || undefined
      });

      setSelectedCase(detail);
      mergeCaseSummary(detail);
      setManualPaymentForm({ mode: "ManualEntry", amount: "", isCardPayment: true, note: "", bankNotificationText: "" });
      setMessage("Final amount recorded.");
    } catch (nextError) {
      showError(nextError);
    }
  }

  async function handleReceiptPayment() {
    if (!session || !selectedCaseId || !receiptPaymentForm.file) {
      return;
    }

    try {
      const file = await compressImageFile(receiptPaymentForm.file);
      const response = await api.addReceiptPayment(session.sessionToken, selectedCaseId, {
        isCardPayment: receiptPaymentForm.isCardPayment,
        note: receiptPaymentForm.note || undefined,
        enteredAmount: receiptPaymentForm.enteredAmount ? Number(receiptPaymentForm.enteredAmount) : undefined,
        file
      });

      setSelectedCase(response.case);
      mergeCaseSummary(response.case);
      setReceiptPaymentForm({ enteredAmount: "", isCardPayment: true, note: "", file: null });
      setMessage("Receipt uploaded. OCR is running in the background.");
    } catch (nextError) {
      showError(nextError);
    }
  }

  async function handleGenerateComplaintPack() {
    if (!session || !selectedCaseId) {
      return;
    }

    try {
      const detail = await api.generateComplaintPack(session.sessionToken, selectedCaseId);
      setSelectedCase(detail);
      mergeCaseSummary(detail);
      setMessage("Complaint pack generated.");
    } catch (nextError) {
      showError(nextError);
    }
  }

  async function handleDownloadComplaintPack() {
    if (!session || !selectedCaseId) {
      return;
    }

    try {
      await api.downloadComplaintPack(session.sessionToken, selectedCaseId);
    } catch (nextError) {
      showError(nextError);
    }
  }

  async function handleQrStub() {
    if (!session) {
      return;
    }

    try {
      const response = await api.getMerchantQrStub(session.sessionToken);
      setQrStubMessage(response.message);
    } catch (nextError) {
      showError(nextError);
    }
  }

  if (!session) {
    return (
      <main className="app-grid min-h-screen px-4 py-6 md:px-10">
        <div className="mx-auto grid max-w-6xl gap-6 lg:grid-cols-[1.2fr_0.8fr]">
          <section className="panel overflow-hidden p-8 md:p-10">
            <div className="inline-flex rounded-full border border-ember/30 bg-ember/10 px-4 py-2 text-sm font-semibold text-ember">
              South African retail price evidence capture
            </div>
            <h1 className="mt-6 max-w-2xl text-4xl font-bold tracking-tight md:text-6xl">
              Build your complaint before the merchant rewrites the story.
            </h1>
            <p className="mt-5 max-w-xl text-base leading-7 text-slate-600 md:text-lg">
              Preserve the quoted amount, compare it to the final charge, classify suspicious differences, and turn the evidence into a complaint-ready PDF.
            </p>
            <div className="mt-8 grid gap-4 md:grid-cols-3">
              <InfoTile title="Capture" body="Manual quotes, shelf photos, audio, and video before payment." />
              <InfoTile title="Compare" body="Final amount entry, receipt OCR, and surcharge classification." />
              <InfoTile title="Escalate" body="Merchant risk history and a downloadable complaint pack." />
            </div>
          </section>

          <section className="panel p-6 md:p-8">
            <div className="flex rounded-full border border-slate-200 bg-slate-100 p-1">
              <button className={clsx("flex-1 rounded-full px-4 py-2 text-sm font-semibold", authMode === "sign-in" ? "bg-white shadow" : "text-slate-500")} onClick={() => setAuthMode("sign-in")} type="button">
                Sign in
              </button>
              <button className={clsx("flex-1 rounded-full px-4 py-2 text-sm font-semibold", authMode === "sign-up" ? "bg-white shadow" : "text-slate-500")} onClick={() => setAuthMode("sign-up")} type="button">
                Create account
              </button>
            </div>

            <div className="mt-6 space-y-4">
              <InputField label="Email" value={email} onChange={setEmail} />
              {authMode === "sign-up" ? <InputField label="Display name" value={displayName} onChange={setDisplayName} /> : null}
              <button className="button-primary w-full" onClick={() => startTransition(() => void handleAuthenticate())} disabled={isPending}>
                {authMode === "sign-up" ? "Create account" : "Sign in"}
              </button>
            </div>
            {error ? <div className="mt-4"><StatusBanner tone="error" message={error} /></div> : null}
          </section>
        </div>
      </main>
    );
  }

  return (
    <main className="app-grid min-h-screen px-4 py-6 md:px-10">
      <div className="mx-auto flex max-w-7xl flex-col gap-6">
        <header className="panel flex flex-col gap-4 p-6 md:flex-row md:items-center md:justify-between">
          <div>
            <p className="text-sm font-semibold uppercase tracking-[0.24em] text-ember">PriceProof SA Console</p>
            <h1 className="mt-2 text-3xl font-bold md:text-4xl">Evidence capture, OCR, discrepancy analysis, and complaint output</h1>
            <p className="mt-2 text-sm text-slate-600">Signed in as {currentUser?.displayName ?? session.displayName} ({currentUser?.role ?? session.role})</p>
          </div>
          <div className="flex flex-wrap gap-3">
            <MetricChip label="Cases" value={String(stats.total)} />
            <MetricChip label="Flagged" value={String(stats.flagged)} tone="warn" />
            <MetricChip label="Ready" value={String(stats.ready)} tone="good" />
            <button className="button-secondary" onClick={() => persistSession(null)} type="button">Sign out</button>
          </div>
        </header>

        {message ? <StatusBanner tone="success" message={message} /> : null}
        {error ? <StatusBanner tone="error" message={error} /> : null}

        <section className="grid gap-6 lg:grid-cols-[320px_1fr]">
          <aside className="space-y-6">
            <div className="panel p-5">
              <h2 className="text-xl font-bold">Start a new case</h2>
              <div className="mt-4 space-y-3">
                <InputField label="Merchant name" value={createCaseForm.merchantName} onChange={(value) => setCreateCaseForm((current) => ({ ...current, merchantName: value }))} />
                <InputField label="Category" value={createCaseForm.merchantCategory} onChange={(value) => setCreateCaseForm((current) => ({ ...current, merchantCategory: value }))} />
                <InputField label="Branch name" value={createCaseForm.branchName} onChange={(value) => setCreateCaseForm((current) => ({ ...current, branchName: value }))} />
                <InputField label="Branch address" value={createCaseForm.branchAddress} onChange={(value) => setCreateCaseForm((current) => ({ ...current, branchAddress: value }))} />
                <InputField label="City" value={createCaseForm.branchCity} onChange={(value) => setCreateCaseForm((current) => ({ ...current, branchCity: value }))} />
                <InputField label="Province" value={createCaseForm.branchProvince} onChange={(value) => setCreateCaseForm((current) => ({ ...current, branchProvince: value }))} />
                <TextAreaField label="Item or basket description" value={createCaseForm.basketDescription} onChange={(value) => setCreateCaseForm((current) => ({ ...current, basketDescription: value }))} />
                <button className="button-primary w-full" onClick={() => startTransition(() => void handleCreateCase())} disabled={isPending}>Create case</button>
              </div>
            </div>

            <div className="panel p-5">
              <div className="flex items-center justify-between">
                <h2 className="text-xl font-bold">Case history</h2>
                <button className="button-secondary px-4 py-2 text-xs" onClick={() => session && startTransition(() => void hydrateDashboard(session))} type="button">Refresh</button>
              </div>
              <div className="mt-4 space-y-3">
                {cases.length === 0 ? (
                  <EmptyState title="No cases yet" body="Create your first case to begin preserving quoted and charged prices." compact />
                ) : (
                  cases.map((item) => (
                    <button
                      key={item.id}
                      className={clsx("w-full rounded-2xl border p-4 text-left transition", selectedCaseId === item.id ? "border-ember bg-ember/5" : "border-slate-200 bg-white hover:border-slate-300")}
                      onClick={() => setSelectedCaseId(item.id)}
                      type="button"
                    >
                      <div className="flex items-center justify-between gap-3">
                        <div>
                          <p className="font-semibold">{item.merchantName}</p>
                          <p className="text-sm text-slate-500">{item.basketDescription}</p>
                        </div>
                        <StatusPill label={item.classification} flagged={item.likelyUnlawfulCardSurcharge} />
                      </div>
                      <div className="mt-3 flex items-center justify-between text-sm text-slate-500">
                        <span>{item.status}</span>
                        <span>{formatMoney(item.differenceAmount)}</span>
                      </div>
                    </button>
                  ))
                )}
              </div>
            </div>
          </aside>

          <section className="space-y-6">
            {!selectedCase ? (
              <div className="panel p-8">
                <EmptyState title="Choose a case" body="Select a case from the sidebar or create a new one to begin the evidence workflow." />
              </div>
            ) : (
              <>
                <div className="panel p-6">
                  <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
                    <div>
                      <p className="text-sm font-semibold uppercase tracking-[0.18em] text-pine">{selectedCase.merchantName}</p>
                      <h2 className="mt-2 text-3xl font-bold">{selectedCase.basketDescription}</h2>
                      <p className="mt-2 text-sm text-slate-500">Created {formatDate(selectedCase.createdAtUtc)} · Updated {formatDate(selectedCase.updatedAtUtc)}</p>
                    </div>
                    <div className="flex flex-wrap gap-3">
                      <StatusPill label={selectedCase.classification} flagged={selectedCase.likelyUnlawfulCardSurcharge} />
                      <MetricChip label="Quoted" value={formatMoney(selectedCase.quotedAmount)} />
                      <MetricChip label="Charged" value={formatMoney(selectedCase.chargedAmount)} />
                      <MetricChip label="Difference" value={formatMoney(selectedCase.differenceAmount)} tone={selectedCase.differenceAmount ? "warn" : "neutral"} />
                    </div>
                  </div>
                  {selectedCase.complaintSummary ? <div className="mt-5 rounded-2xl bg-slate-50 p-4 text-sm leading-6 text-slate-700">{selectedCase.complaintSummary}</div> : null}
                  <div className="mt-5 flex flex-wrap gap-3">
                    <button className="button-primary" onClick={() => startTransition(() => void handleGenerateComplaintPack())} disabled={isPending}>Generate complaint pack</button>
                    <button className="button-secondary" onClick={() => startTransition(() => void handleDownloadComplaintPack())} disabled={!selectedCase.latestComplaintPack}>Download latest PDF</button>
                    <button className="button-secondary" onClick={() => startTransition(() => void handleQrStub())} type="button">Merchant QR lock stub</button>
                  </div>
                  {qrStubMessage ? <p className="mt-4 text-sm text-slate-500">{qrStubMessage}</p> : null}
                </div>

                <div className="grid gap-6 xl:grid-cols-2">
                  <div className="panel p-6">
                    <h3 className="text-2xl font-bold">Capture price before payment</h3>
                    <div className="mt-5 grid gap-5">
                      <div className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
                        <h4 className="text-lg font-semibold">Manual quoted price</h4>
                        <div className="mt-3 space-y-3">
                          <InputField label="Quoted amount (R)" type="number" value={manualPriceForm.amount} onChange={(value) => setManualPriceForm((current) => ({ ...current, amount: value }))} />
                          <InputField label="Quote text" value={manualPriceForm.quoteText} onChange={(value) => setManualPriceForm((current) => ({ ...current, quoteText: value }))} />
                          <TextAreaField label="Notes" value={manualPriceForm.notes} onChange={(value) => setManualPriceForm((current) => ({ ...current, notes: value }))} />
                          <button className="button-primary w-full" onClick={() => startTransition(() => void handleManualPriceCapture())} disabled={isPending}>Save quoted price</button>
                        </div>
                      </div>

                      <div className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
                        <h4 className="text-lg font-semibold">Photo, audio, or video evidence</h4>
                        <div className="mt-3 space-y-3">
                          <SelectField label="Capture mode" value={mediaPriceForm.mode} onChange={(value) => setMediaPriceForm((current) => ({ ...current, mode: value }))} options={mediaModes} />
                          <InputField label="Optional amount (R)" type="number" value={mediaPriceForm.amount} onChange={(value) => setMediaPriceForm((current) => ({ ...current, amount: value }))} />
                          <InputField label="Quote text" value={mediaPriceForm.quoteText} onChange={(value) => setMediaPriceForm((current) => ({ ...current, quoteText: value }))} />
                          <TextAreaField label="Notes" value={mediaPriceForm.notes} onChange={(value) => setMediaPriceForm((current) => ({ ...current, notes: value }))} />
                          <FileField label="Upload media" accept="image/*,audio/*,video/*" onChange={(file) => setMediaPriceForm((current) => ({ ...current, file }))} />
                          <button className="button-primary w-full" onClick={() => startTransition(() => void handleMediaPriceCapture())} disabled={isPending}>Upload pre-payment evidence</button>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="panel p-6">
                    <h3 className="text-2xl font-bold">Capture the final payment proof</h3>
                    <div className="mt-5 grid gap-5">
                      <div className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
                        <h4 className="text-lg font-semibold">Manual or bank notification entry</h4>
                        <div className="mt-3 space-y-3">
                          <SelectField label="Payment mode" value={manualPaymentForm.mode} onChange={(value) => setManualPaymentForm((current) => ({ ...current, mode: value }))} options={paymentModes} />
                          <InputField label="Charged amount (R)" type="number" value={manualPaymentForm.amount} onChange={(value) => setManualPaymentForm((current) => ({ ...current, amount: value }))} />
                          <ToggleField label="Paid by card" checked={manualPaymentForm.isCardPayment} onChange={(checked) => setManualPaymentForm((current) => ({ ...current, isCardPayment: checked }))} />
                          <InputField label="Payment note" value={manualPaymentForm.note} onChange={(value) => setManualPaymentForm((current) => ({ ...current, note: value }))} />
                          {manualPaymentForm.mode === "BankNotification" ? <TextAreaField label="Bank notification text" value={manualPaymentForm.bankNotificationText} onChange={(value) => setManualPaymentForm((current) => ({ ...current, bankNotificationText: value }))} /> : null}
                          <button className="button-primary w-full" onClick={() => startTransition(() => void handleManualPayment())} disabled={isPending}>Save final amount</button>
                        </div>
                      </div>

                      <div className="rounded-3xl border border-slate-200 bg-slate-50 p-4">
                        <h4 className="text-lg font-semibold">Receipt upload</h4>
                        <div className="mt-3 space-y-3">
                          <InputField label="Optional entered amount (R)" type="number" value={receiptPaymentForm.enteredAmount} onChange={(value) => setReceiptPaymentForm((current) => ({ ...current, enteredAmount: value }))} />
                          <ToggleField label="Paid by card" checked={receiptPaymentForm.isCardPayment} onChange={(checked) => setReceiptPaymentForm((current) => ({ ...current, isCardPayment: checked }))} />
                          <InputField label="Payment note" value={receiptPaymentForm.note} onChange={(value) => setReceiptPaymentForm((current) => ({ ...current, note: value }))} />
                          <FileField label="Upload receipt" accept="image/*,.pdf,.txt" onChange={(file) => setReceiptPaymentForm((current) => ({ ...current, file }))} />
                          <button className="button-primary w-full" onClick={() => startTransition(() => void handleReceiptPayment())} disabled={isPending}>Upload receipt and start OCR</button>
                          <p className="text-xs leading-5 text-slate-500">For a quick local demo, use a file name like <code>receipt-59.99.jpg</code> to help the mock OCR infer a total.</p>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>

                <div className="grid gap-6 xl:grid-cols-[1fr_360px]">
                  <div className="panel p-6">
                    <h3 className="text-2xl font-bold">Evidence timeline</h3>
                    <div className="mt-5 grid gap-4">
                      {selectedCase.priceCaptures.length === 0 && selectedCase.paymentRecords.length === 0 ? (
                        <EmptyState title="No evidence yet" body="Add the quoted amount or payment proof to populate the case timeline." compact />
                      ) : (
                        <>
                          {selectedCase.priceCaptures.map((capture) => (
                            <TimelineCard
                              key={capture.id}
                              title={`Price capture · ${capture.mode}`}
                              subtitle={formatDate(capture.capturedAtUtc)}
                              body={`${formatMoney(capture.capturedAmount)} · ${capture.quoteText ?? "No quote text supplied"}`}
                              meta={capture.evidence.length > 0 ? `${capture.evidence.length} attachment(s)` : "No attachments"}
                            />
                          ))}
                          {selectedCase.paymentRecords.map((payment) => (
                            <TimelineCard
                              key={payment.id}
                              title={`Payment record · ${payment.mode}`}
                              subtitle={formatDate(payment.capturedAtUtc)}
                              body={`${formatMoney(payment.amount)} · ${payment.note ?? "No note supplied"}`}
                              meta={payment.receipt ? `Receipt OCR: ${payment.receipt.ocrStatus}` : "No receipt attached"}
                            />
                          ))}
                        </>
                      )}
                    </div>
                  </div>

                  <div className="space-y-6">
                    <div className="panel p-6">
                      <h3 className="text-2xl font-bold">Merchant risk history</h3>
                      {merchantHistory ? (
                        <div className="mt-4 space-y-4">
                          <div className="rounded-2xl bg-slate-50 p-4">
                            <p className="text-sm text-slate-500">Trend</p>
                            <p className="mt-1 text-2xl font-bold">{merchantHistory.risk?.trend ?? "Not enough data"}</p>
                            <p className="mt-1 text-sm text-slate-600">Risk score {merchantHistory.risk?.score ?? 0} from {merchantHistory.totalCases} case(s).</p>
                          </div>
                          <div className="space-y-3">
                            {merchantHistory.recentCases.slice(0, 4).map((item) => (
                              <div key={item.id} className="rounded-2xl border border-slate-200 p-3">
                                <div className="flex items-center justify-between gap-3">
                                  <p className="font-semibold">{item.basketDescription}</p>
                                  <StatusPill label={item.classification} flagged={item.likelyUnlawfulCardSurcharge} />
                                </div>
                                <p className="mt-2 text-sm text-slate-500">{formatDate(item.updatedAtUtc)}</p>
                              </div>
                            ))}
                          </div>
                        </div>
                      ) : (
                        <div className="mt-4">
                          <EmptyState title="Risk history will appear here" body="Merchant scoring updates as more reports are classified." compact />
                        </div>
                      )}
                    </div>

                    <div className="panel p-6">
                      <h3 className="text-2xl font-bold">Complaint pack</h3>
                      {selectedCase.latestComplaintPack ? (
                        <div className="mt-4 space-y-3">
                          <p className="text-sm text-slate-600">{selectedCase.latestComplaintPack.summary}</p>
                          <p className="text-sm text-slate-500">Generated {formatDate(selectedCase.latestComplaintPack.generatedAtUtc)}</p>
                          <button className="button-primary w-full" onClick={() => startTransition(() => void handleDownloadComplaintPack())}>Download PDF evidence pack</button>
                        </div>
                      ) : (
                        <div className="mt-4">
                          <EmptyState title="No PDF yet" body="Generate a complaint pack once the case has both price and payment evidence." compact />
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              </>
            )}
          </section>
        </section>
      </div>
    </main>
  );
}
