import type {
  AuthSession,
  CaseDetail,
  CaseSummary,
  CurrentUser,
  MerchantHistory,
  QrQuoteLockStubResponse,
  ReceiptUploadResponse
} from "@/lib/types";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:8081";

async function apiRequest<T>(path: string, options: RequestInit = {}, sessionToken?: string) {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers: {
      ...(sessionToken ? { Authorization: `Bearer ${sessionToken}` } : {}),
      ...(options.body instanceof FormData ? {} : { "Content-Type": "application/json" }),
      ...options.headers
    }
  });

  if (!response.ok) {
    const fallbackMessage = `Request failed with status ${response.status}.`;
    let message = fallbackMessage;
    try {
      const body = (await response.json()) as { title?: string };
      message = body.title ?? fallbackMessage;
    } catch {
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const api = {
  signUp: (input: { email: string; displayName: string }) =>
    apiRequest<AuthSession>("/api/auth/sign-up", {
      method: "POST",
      body: JSON.stringify(input)
    }),
  signIn: (input: { email: string }) =>
    apiRequest<AuthSession>("/api/auth/sign-in", {
      method: "POST",
      body: JSON.stringify(input)
    }),
  getCurrentUser: (token: string) => apiRequest<CurrentUser>("/api/auth/me", {}, token),
  listCases: (token: string) => apiRequest<CaseSummary[]>("/api/cases", {}, token),
  createCase: (
    token: string,
    input: {
      merchantName: string;
      merchantCategory?: string;
      branchName?: string;
      branchAddress?: string;
      branchCity?: string;
      branchProvince?: string;
      basketDescription: string;
    }
  ) =>
    apiRequest<CaseDetail>("/api/cases", {
      method: "POST",
      body: JSON.stringify(input)
    }, token),
  getCase: (token: string, caseId: string) => apiRequest<CaseDetail>(`/api/cases/${caseId}`, {}, token),
  addManualPriceCapture: (
    token: string,
    caseId: string,
    input: { amount: number; quoteText?: string; notes?: string }
  ) =>
    apiRequest<CaseDetail>(`/api/cases/${caseId}/price-captures/manual`, {
      method: "POST",
      body: JSON.stringify(input)
    }, token),
  addMediaPriceCapture: async (
    token: string,
    caseId: string,
    input: { mode: string; amount?: number; quoteText?: string; notes?: string; file: File }
  ) => {
    const formData = new FormData();
    formData.append("mode", input.mode);
    if (typeof input.amount === "number") {
      formData.append("amount", String(input.amount));
    }
    if (input.quoteText) {
      formData.append("quoteText", input.quoteText);
    }
    if (input.notes) {
      formData.append("notes", input.notes);
    }
    formData.append("file", input.file);

    return apiRequest<CaseDetail>(`/api/cases/${caseId}/price-captures/media`, {
      method: "POST",
      body: formData
    }, token);
  },
  addManualPayment: (
    token: string,
    caseId: string,
    input: {
      amount: number;
      mode: string;
      isCardPayment: boolean;
      note?: string;
      bankNotificationText?: string;
    }
  ) =>
    apiRequest<CaseDetail>(`/api/cases/${caseId}/payments/manual`, {
      method: "POST",
      body: JSON.stringify(input)
    }, token),
  addReceiptPayment: async (
    token: string,
    caseId: string,
    input: { isCardPayment: boolean; note?: string; enteredAmount?: number; file: File }
  ) => {
    const formData = new FormData();
    formData.append("isCardPayment", String(input.isCardPayment));
    if (typeof input.enteredAmount === "number") {
      formData.append("enteredAmount", String(input.enteredAmount));
    }
    if (input.note) {
      formData.append("note", input.note);
    }
    formData.append("file", input.file);

    return apiRequest<ReceiptUploadResponse>(`/api/cases/${caseId}/payments/receipt`, {
      method: "POST",
      body: formData
    }, token);
  },
  generateComplaintPack: (token: string, caseId: string) =>
    apiRequest<CaseDetail>(`/api/cases/${caseId}/complaint-pack`, {
      method: "POST"
    }, token),
  getMerchantHistory: (token: string, merchantId: string) =>
    apiRequest<MerchantHistory>(`/api/merchants/${merchantId}/history`, {}, token),
  getMerchantQrStub: (token: string) => apiRequest<QrQuoteLockStubResponse>("/api/cases/merchant-qr-lock-stub", {}, token),
  async downloadComplaintPack(token: string, caseId: string) {
    const response = await fetch(`${API_BASE_URL}/api/cases/${caseId}/complaint-pack/download`, {
      headers: {
        Authorization: `Bearer ${token}`
      }
    });

    if (!response.ok) {
      throw new Error("Failed to download the complaint pack.");
    }

    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    const disposition = response.headers.get("Content-Disposition");
    const fileName = disposition?.match(/filename="?([^"]+)"?/)?.[1] ?? "priceproof-evidence-pack.pdf";
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
  }
};
