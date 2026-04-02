import type {
  AnalyzeCaseRequest,
  AuthSession,
  BranchRisk,
  BootstrapLookups,
  CaseAnalysis,
  CaseClassification,
  CaseDetail,
  CaseSummary,
  CreateCaseRequest,
  CreatePaymentRecordRequest,
  CreatePriceCaptureRequest,
  CreateReceiptRecordRequest,
  CurrentUser,
  GeneratedComplaintPack,
  GetCasesQuery,
  MerchantRisk,
  MerchantHistory,
  PagedResult,
  PaymentRecordSummary,
  PriceCaptureSummary,
  RiskOverview,
  ReceiptSummary,
  RunReceiptOcrResult,
  SignInRequest,
  SignUpRequest,
  UploadedFile
} from "@/lib/types";

const API_BASE_URL = "";
const API_PREFIX = "/backend";

type QueryValue = string | number | boolean | undefined | null;

export class ApiError extends Error {
  status: number;
  traceId?: string;
  fieldErrors?: Record<string, string[]>;

  constructor(message: string, status: number, traceId?: string, fieldErrors?: Record<string, string[]>) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.traceId = traceId;
    this.fieldErrors = fieldErrors;
  }
}

async function requestJson<T>(path: string, init: RequestInit = {}) {
  const response = await fetch(`${API_PREFIX}${path}`, {
    ...init,
    headers: {
      Accept: "application/json",
      ...(init.body instanceof FormData ? {} : { "Content-Type": "application/json" }),
      ...init.headers
    },
    cache: "no-store"
  });

  if (!response.ok) {
    let message = `Request failed with status ${response.status}.`;
    let traceId: string | undefined;
    let fieldErrors: Record<string, string[]> | undefined;

    try {
      const payload = (await response.json()) as {
        title?: string;
        detail?: string;
        traceId?: string;
        errors?: Record<string, string[]>;
      };

      message = payload.detail ?? payload.title ?? message;
      traceId = payload.traceId;
      fieldErrors = payload.errors;
    } catch {
    }

    throw new ApiError(message, response.status, traceId, fieldErrors);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

function withQuery(path: string, query?: Record<string, QueryValue>) {
  if (!query) {
    return path;
  }

  const searchParams = new URLSearchParams();

  Object.entries(query).forEach(([key, value]) => {
    if (value === undefined || value === null || value === "") {
      return;
    }

    searchParams.set(key, String(value));
  });

  const suffix = searchParams.toString();
  return suffix ? `${path}?${suffix}` : path;
}

export function getApiBaseUrl() {
  return API_PREFIX;
}

export function buildAbsoluteUrl(path: string) {
  if (/^https?:\/\//i.test(path)) {
    return path;
  }

  const normalizedPath = path.startsWith("/") ? path : `/${path}`;
  return `${API_PREFIX}${normalizedPath}`;
}

export function buildUploadContentUrl(storagePath: string) {
  return buildAbsoluteUrl(`/uploads/content?path=${encodeURIComponent(storagePath)}`);
}

async function downloadBinary(path: string, fallbackFileName: string) {
  const response = await fetch(buildAbsoluteUrl(path), {
    cache: "no-store"
  });

  if (!response.ok) {
    throw new ApiError("Unable to download the requested document.", response.status);
  }

  const blob = await response.blob();
  const downloadUrl = window.URL.createObjectURL(blob);
  const link = window.document.createElement("a");
  const contentDisposition = response.headers.get("Content-Disposition");
  const suggestedFileName = contentDisposition?.match(/filename=\"?([^\"]+)\"?/)?.[1] ?? fallbackFileName;

  link.href = downloadUrl;
  link.download = suggestedFileName;
  link.click();

  window.URL.revokeObjectURL(downloadUrl);
}

export const api = {
  signIn(input: SignInRequest) {
    return requestJson<AuthSession>("/auth/sign-in", {
      method: "POST",
      body: JSON.stringify(input)
    });
  },
  signUp(input: SignUpRequest) {
    return requestJson<AuthSession>("/auth/sign-up", {
      method: "POST",
      body: JSON.stringify(input)
    });
  },
  getCurrentUser(userId: string) {
    return requestJson<CurrentUser>(`/auth/me/${userId}`);
  },
  getBootstrapLookups() {
    return requestJson<BootstrapLookups>("/lookups/bootstrap");
  },
  listCases(query: GetCasesQuery) {
    return requestJson<PagedResult<CaseSummary>>(withQuery("/cases", query));
  },
  getCase(caseId: string) {
    return requestJson<CaseDetail>(`/cases/${caseId}`);
  },
  createCase(input: CreateCaseRequest) {
    return requestJson<CaseDetail>("/cases", {
      method: "POST",
      body: JSON.stringify(input)
    });
  },
  createPriceCapture(input: CreatePriceCaptureRequest) {
    return requestJson<PriceCaptureSummary>("/price-captures", {
      method: "POST",
      body: JSON.stringify(input)
    });
  },
  createPaymentRecord(input: CreatePaymentRecordRequest) {
    return requestJson<PaymentRecordSummary>("/payment-records", {
      method: "POST",
      body: JSON.stringify(input)
    });
  },
  createReceiptRecord(input: CreateReceiptRecordRequest) {
    return requestJson<ReceiptSummary>("/receipt-records", {
      method: "POST",
      body: JSON.stringify(input)
    });
  },
  runReceiptOcr(receiptRecordId: string) {
    return requestJson<RunReceiptOcrResult>(`/receipt-records/${receiptRecordId}/run-ocr`, {
      method: "POST"
    });
  },
  analyzeCase(caseId: string, input: AnalyzeCaseRequest) {
    return requestJson<CaseAnalysis>(`/cases/${caseId}/analyze`, {
      method: "POST",
      body: JSON.stringify(input)
    });
  },
  generateComplaintPack(caseId: string) {
    return requestJson<GeneratedComplaintPack>(`/cases/${caseId}/generate-complaint-pack`, {
      method: "POST"
    });
  },
  getMerchantHistory(merchantId: string) {
    return requestJson<MerchantHistory>(`/merchants/${merchantId}/history`);
  },
  getMerchantRisk(merchantId: string) {
    return requestJson<MerchantRisk>(`/merchants/${merchantId}/risk`);
  },
  getBranchRisk(branchId: string) {
    return requestJson<BranchRisk>(`/branches/${branchId}/risk`);
  },
  getRiskOverview(requestedByUserId: string) {
    return requestJson<RiskOverview>(withQuery("/risk/overview", { requestedByUserId }));
  },
  uploadFile(file: File, category: string, caseId?: string) {
    const formData = new FormData();
    formData.set("file", file);
    formData.set("category", category);

    if (caseId) {
      formData.set("caseId", caseId);
    }

    return requestJson<UploadedFile>("/uploads", {
      method: "POST",
      body: formData
    });
  },
  downloadComplaintPack(complaintPackId: string, fallbackFileName = "priceproof-complaint-pack.pdf") {
    return downloadBinary(`/complaint-packs/${complaintPackId}/download`, fallbackFileName);
  }
};

export const caseClassificationOptions: CaseClassification[] = [
  "PendingEvidence",
  "Match",
  "Undercharge",
  "Overcharge",
  "PotentialCardSurcharge",
  "NeedsReview"
];
