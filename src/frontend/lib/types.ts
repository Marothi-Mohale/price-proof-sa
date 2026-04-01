export type CaptureType =
  | "ManualEntry"
  | "ShelfPhoto"
  | "PriceTagPhoto"
  | "WebsiteListing"
  | "ChatMessage"
  | "QuotationDocument"
  | "MerchantQrQuote";

export type EvidenceType = "Image" | "Pdf" | "Audio" | "Video" | "Text" | "Json";

export type PaymentMethod =
  | "Unknown"
  | "Cash"
  | "DebitCard"
  | "CreditCard"
  | "BankTransfer"
  | "Wallet";

export type CaseClassification =
  | "PendingEvidence"
  | "Match"
  | "Undercharge"
  | "Overcharge"
  | "PotentialCardSurcharge"
  | "NeedsReview";

export type CaseStatus = "Open" | "AwaitingPayment" | "AwaitingReceipt" | "ReadyForReview" | "Closed";

export type AnalysisClassification =
  | "Match"
  | "LikelyCardSurcharge"
  | "PossibleCashback"
  | "PossibleSeparateFee"
  | "UnclearPositiveMismatch"
  | "LowerThanQuoted";

export type ComplaintEvidenceStrength = "Strong" | "Moderate" | "Weak";

export type AuthSession = {
  userId: string;
  email: string;
  displayName: string;
  isActive: boolean;
  signedInAtUtc: string;
};

export type CurrentUser = {
  id: string;
  email: string;
  displayName: string;
  isActive: boolean;
};

export type LookupUser = {
  id: string;
  displayName: string;
  email: string;
  isActive: boolean;
};

export type LookupBranch = {
  id: string;
  merchantId: string;
  name: string;
  code?: string | null;
  addressLine1: string;
  addressLine2?: string | null;
  city: string;
  province: string;
  postalCode?: string | null;
};

export type LookupMerchant = {
  id: string;
  name: string;
  category?: string | null;
  websiteUrl?: string | null;
  branches: LookupBranch[];
};

export type BootstrapLookups = {
  users: LookupUser[];
  merchants: LookupMerchant[];
};

export type UserReference = {
  id: string;
  displayName: string;
  email: string;
};

export type MerchantReference = {
  id: string;
  name: string;
  category?: string | null;
  websiteUrl?: string | null;
};

export type BranchReference = {
  id: string;
  merchantId: string;
  name: string;
  code?: string | null;
  addressLine1: string;
  addressLine2?: string | null;
  city: string;
  province: string;
  postalCode?: string | null;
};

export type PriceCaptureSummary = {
  id: string;
  captureType: CaptureType | string;
  evidenceType: EvidenceType | string;
  quotedAmount?: number | null;
  currencyCode: string;
  fileName: string;
  contentType?: string | null;
  evidenceStoragePath: string;
  merchantStatement?: string | null;
  notes?: string | null;
  capturedAtUtc: string;
  createdUtc: string;
};

export type ReceiptSummary = {
  id: string;
  evidenceType: EvidenceType | string;
  fileName: string;
  contentType: string;
  storagePath: string;
  currencyCode: string;
  parsedTotalAmount?: number | null;
  receiptNumber?: string | null;
  merchantName?: string | null;
  rawText?: string | null;
  uploadedAtUtc: string;
  createdUtc: string;
};

export type PaymentRecordSummary = {
  id: string;
  paymentMethod: PaymentMethod | string;
  amount: number;
  currencyCode: string;
  paymentReference?: string | null;
  merchantReference?: string | null;
  cardLastFour?: string | null;
  notes?: string | null;
  paidAtUtc: string;
  createdUtc: string;
  receipt?: ReceiptSummary | null;
};

export type ComplaintPackSummary = {
  id: string;
  fileName: string;
  storagePath: string;
  contentHash: string;
  summary: string;
  downloadUrl: string;
  generatedAtUtc: string;
};

export type AuditLog = {
  id: string;
  entityName: string;
  action: string;
  correlationId: string;
  occurredAtUtc: string;
  createdUtc: string;
};

export type CaseAnalysis = {
  quotedAmount: number;
  chargedAmount: number;
  difference: number;
  percentageDifference?: number | null;
  classification: AnalysisClassification | string;
  confidence: number;
  explanation: string;
};

export type CaseSummary = {
  id: string;
  caseNumber: string;
  merchant: MerchantReference;
  branch?: BranchReference | null;
  basketDescription: string;
  status: CaseStatus | string;
  classification: CaseClassification | string;
  latestQuotedAmount?: number | null;
  latestPaidAmount?: number | null;
  differenceAmount?: number | null;
  incidentAtUtc: string;
  createdUtc: string;
  updatedUtc: string;
};

export type CaseDetail = {
  id: string;
  caseNumber: string;
  reportedBy: UserReference;
  merchant: MerchantReference;
  branch?: BranchReference | null;
  basketDescription: string;
  currencyCode: string;
  status: CaseStatus | string;
  classification: CaseClassification | string;
  latestQuotedAmount?: number | null;
  latestPaidAmount?: number | null;
  differenceAmount?: number | null;
  incidentAtUtc: string;
  customerReference?: string | null;
  notes?: string | null;
  createdUtc: string;
  updatedUtc: string;
  priceCaptures: PriceCaptureSummary[];
  paymentRecords: PaymentRecordSummary[];
  complaintPacks: ComplaintPackSummary[];
  auditLogs: AuditLog[];
};

export type MerchantHistory = {
  merchantId: string;
  merchantName: string;
  category?: string | null;
  websiteUrl?: string | null;
  totalCases: number;
  potentialCardSurchargeCases: number;
  needsReviewCases: number;
  matchCases: number;
  recentCases: CaseSummary[];
};

export type UploadedFile = {
  fileName: string;
  contentType: string;
  storagePath: string;
  contentHash: string;
  sizeBytes: number;
};

export type PagedResult<T> = {
  items: T[];
  totalCount: number;
  skip: number;
  take: number;
};

export type ReceiptOcrLineItem = {
  description: string;
  totalAmount?: number | null;
  quantity?: number | null;
  unitPrice?: number | null;
};

export type RunReceiptOcrResult = {
  receiptRecordId: string;
  providerName: string;
  confidence: number;
  rawPayloadMetadataJson: string;
  merchantName?: string | null;
  transactionTotal?: number | null;
  transactionAtUtc?: string | null;
  lineItems: ReceiptOcrLineItem[];
  rawText?: string | null;
  processedAtUtc: string;
};

export type ComplaintPackLocation = {
  merchantName: string;
  merchantCategory?: string | null;
  merchantWebsiteUrl?: string | null;
  branchName?: string | null;
  branchCode?: string | null;
  addressLine1?: string | null;
  addressLine2?: string | null;
  city?: string | null;
  province?: string | null;
  postalCode?: string | null;
};

export type ComplaintPackAmounts = {
  currencyCode: string;
  quotedAmount: number;
  chargedAmount: number;
  discrepancyAmount: number;
  percentageDifference?: number | null;
};

export type ComplaintPackAnalysis = {
  classification: string;
  classificationLabel: string;
  confidence?: number | null;
  explanation: string;
};

export type ComplaintPackEvidenceAssessment = {
  strength: ComplaintEvidenceStrength | string;
  explanation: string;
};

export type ComplaintPackTimelineItem = {
  occurredAtUtc: string;
  title: string;
  description: string;
};

export type ComplaintPackEvidenceItem = {
  category: string;
  label: string;
  fileName: string;
  contentType?: string | null;
  storagePath: string;
  referenceLink?: string | null;
  recordedAtUtc: string;
  currencyCode: string;
  amount?: number | null;
  notes?: string | null;
};

export type ComplaintPackJsonSummary = {
  caseId: string;
  caseReferenceNumber: string;
  location: ComplaintPackLocation;
  amounts: ComplaintPackAmounts;
  analysis: ComplaintPackAnalysis;
  evidenceAssessment: ComplaintPackEvidenceAssessment;
  timeline: ComplaintPackTimelineItem[];
  evidenceInventory: ComplaintPackEvidenceItem[];
  complaintSummary: string;
  declarationText: string;
  auditTimestampUtc: string;
};

export type GeneratedComplaintPack = {
  id: string;
  caseId: string;
  caseReferenceNumber: string;
  fileName: string;
  contentType: string;
  downloadUrl: string;
  contentHash: string;
  fileSizeBytes: number;
  summary: string;
  jsonSummary: ComplaintPackJsonSummary;
  generatedAtUtc: string;
};

export type SignInRequest = {
  email: string;
};

export type SignUpRequest = {
  email: string;
  displayName: string;
};

export type GetCasesQuery = {
  merchantId?: string;
  reportedByUserId?: string;
  classification?: CaseClassification;
  skip?: number;
  take?: number;
};

export type CreateCaseRequest = {
  reportedByUserId: string;
  merchantId: string;
  branchId?: string | null;
  basketDescription: string;
  incidentAtUtc: string;
  currencyCode: string;
  customerReference?: string | null;
  notes?: string | null;
};

export type CreatePriceCaptureRequest = {
  caseId: string;
  capturedByUserId: string;
  captureType: CaptureType;
  evidenceType: EvidenceType;
  quotedAmount?: number | null;
  currencyCode: string;
  fileName: string;
  evidenceStoragePath: string;
  capturedAtUtc: string;
  contentType?: string | null;
  evidenceHash?: string | null;
  merchantStatement?: string | null;
  notes?: string | null;
};

export type CreatePaymentRecordRequest = {
  caseId: string;
  recordedByUserId: string;
  paymentMethod: PaymentMethod;
  amount: number;
  currencyCode: string;
  paidAtUtc: string;
  paymentReference?: string | null;
  merchantReference?: string | null;
  cardLastFour?: string | null;
  notes?: string | null;
};

export type CreateReceiptRecordRequest = {
  caseId: string;
  paymentRecordId: string;
  uploadedByUserId: string;
  evidenceType: EvidenceType;
  fileName: string;
  contentType: string;
  storagePath: string;
  uploadedAtUtc: string;
  currencyCode: string;
  parsedTotalAmount?: number | null;
  receiptNumber?: string | null;
  merchantName?: string | null;
  rawText?: string | null;
  fileHash?: string | null;
};

export type AnalyzeCaseRequest = {
  merchantSaidCardFee?: boolean;
  cashbackPresent?: boolean;
  deliveryOrServiceFeePresent?: boolean;
  evidenceText?: string | null;
};

export type AppPreferences = {
  preferredCurrency: string;
  autoRunOcr: boolean;
  autoAnalyzeCase: boolean;
};
