import { z } from "zod";

export const signInSchema = z.object({
  email: z.string().trim().email("Enter a valid email address.").max(320)
});

export const signUpSchema = z.object({
  displayName: z.string().trim().min(2, "Enter your name.").max(120),
  email: z.string().trim().email("Enter a valid email address.").max(320)
});

export const caseDetailsSchema = z.object({
  merchantId: z.string().uuid("Choose a merchant."),
  branchId: z.string().uuid("Choose a branch.").optional().or(z.literal("")),
  basketDescription: z.string().trim().min(3, "Describe the basket or item.").max(500),
  incidentAtLocal: z.string().min(1, "Choose when the incident happened."),
  currencyCode: z.string().trim().length(3, "Use a 3-letter currency code."),
  customerReference: z.string().trim().max(64).optional(),
  notes: z.string().trim().max(2000).optional()
});

export const priceEvidenceSchema = z.object({
  quotedAmount: z.string().trim().optional(),
  capturedAtLocal: z.string().min(1, "Choose when the quoted price was captured."),
  merchantStatement: z.string().trim().max(2000).optional(),
  notes: z.string().trim().max(2000).optional()
});

export const paymentEvidenceSchema = z.object({
  amount: z.string().trim().min(1, "Enter the charged amount."),
  paymentMethod: z.string().trim().min(1, "Choose how the payment was made."),
  paidAtLocal: z.string().min(1, "Choose when the payment happened."),
  paymentReference: z.string().trim().max(64).optional(),
  merchantReference: z.string().trim().max(64).optional(),
  cardLastFour: z.string().trim().max(4).optional(),
  notes: z.string().trim().max(2000).optional(),
  parsedTotalAmount: z.string().trim().optional(),
  receiptNumber: z.string().trim().max(64).optional(),
  merchantName: z.string().trim().max(200).optional(),
  rawText: z.string().trim().max(16000).optional()
});

export const analysisSchema = z.object({
  merchantSaidCardFee: z.boolean(),
  cashbackPresent: z.boolean(),
  deliveryOrServiceFeePresent: z.boolean(),
  evidenceText: z.string().trim().max(4000).optional()
});

export const preferencesSchema = z.object({
  preferredCurrency: z.string().trim().length(3, "Use a 3-letter currency code."),
  autoRunOcr: z.boolean(),
  autoAnalyzeCase: z.boolean()
});

export function validatePositiveMoney(value: string, fieldLabel: string) {
  const trimmed = value.trim();

  if (!trimmed) {
    return `${fieldLabel} is required.`;
  }

  const numericValue = Number(trimmed);
  if (!Number.isFinite(numericValue) || numericValue <= 0) {
    return `${fieldLabel} must be greater than zero.`;
  }

  return undefined;
}

export function flattenZodErrors(issues: z.ZodIssue[]) {
  return issues.reduce<Record<string, string>>((errors, issue) => {
    const key = issue.path[0]?.toString() ?? "form";

    if (!errors[key]) {
      errors[key] = issue.message;
    }

    return errors;
  }, {});
}
