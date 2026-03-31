export type AuthSession = {
  userId: string;
  email: string;
  displayName: string;
  role: string;
  sessionToken: string;
  expiresAtUtc: string;
};

export type CurrentUser = {
  id: string;
  email: string;
  displayName: string;
  role: string;
};

export type CaseSummary = {
  id: string;
  merchantName: string;
  basketDescription: string;
  status: string;
  quotedAmount: number | null;
  chargedAmount: number | null;
  differenceAmount: number | null;
  classification: string;
  likelyUnlawfulCardSurcharge: boolean;
  updatedAtUtc: string;
};

export type Branch = {
  id: string;
  name: string;
  addressLine: string | null;
  city: string | null;
  province: string | null;
};

export type PriceEvidence = {
  id: string;
  fileType: string;
  fileName: string;
  contentType: string;
  storagePath: string;
  uploadedAtUtc: string;
};

export type PriceCapture = {
  id: string;
  mode: string;
  capturedAmount: number | null;
  quoteText: string | null;
  notes: string | null;
  capturedAtUtc: string;
  evidence: PriceEvidence[];
};

export type ReceiptRecord = {
  id: string;
  fileName: string;
  ocrStatus: string;
  parsedTotalAmount: number | null;
  processedAtUtc: string | null;
  retryCount: number;
};

export type PaymentRecord = {
  id: string;
  mode: string;
  amount: number | null;
  isCardPayment: boolean;
  note: string | null;
  redactedBankNotificationText: string | null;
  capturedAtUtc: string;
  receipt: ReceiptRecord | null;
};

export type ComplaintPack = {
  id: string;
  fileName: string;
  storagePath: string;
  summary: string;
  generatedAtUtc: string;
};

export type MerchantRisk = {
  totalReports: number;
  confirmedSurchargeSignals: number;
  score: number;
  trend: string;
  lastCalculatedAtUtc: string;
};

export type CaseDetail = {
  id: string;
  merchantId: string;
  merchantName: string;
  branch: Branch | null;
  basketDescription: string;
  status: string;
  quotedAmount: number | null;
  chargedAmount: number | null;
  differenceAmount: number | null;
  classification: string;
  likelyUnlawfulCardSurcharge: boolean;
  complaintSummary: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  priceCaptures: PriceCapture[];
  paymentRecords: PaymentRecord[];
  latestComplaintPack: ComplaintPack | null;
  merchantRisk: MerchantRisk | null;
};

export type MerchantHistory = {
  merchantId: string;
  merchantName: string;
  risk: MerchantRisk | null;
  totalCases: number;
  casesFlaggedAsLikelyCardSurcharge: number;
  recentCases: CaseSummary[];
};

export type ReceiptUploadResponse = {
  receiptId: string;
  case: CaseDetail;
};

export type QrQuoteLockStubResponse = {
  available: boolean;
  message: string;
};
