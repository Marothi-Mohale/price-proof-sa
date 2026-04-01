export function formatCurrency(amount?: number | null, currencyCode = "ZAR") {
  if (typeof amount !== "number" || Number.isNaN(amount)) {
    return "Not recorded";
  }

  try {
    return new Intl.NumberFormat("en-ZA", {
      style: "currency",
      currency: currencyCode.toUpperCase(),
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(amount);
  } catch {
    return `${currencyCode.toUpperCase()} ${amount.toFixed(2)}`;
  }
}

export function formatDateTime(value?: string | null) {
  if (!value) {
    return "Not recorded";
  }

  return new Intl.DateTimeFormat("en-ZA", {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

export function formatDate(value?: string | null) {
  if (!value) {
    return "Not recorded";
  }

  return new Intl.DateTimeFormat("en-ZA", {
    dateStyle: "medium"
  }).format(new Date(value));
}

export function formatPercentage(value?: number | null) {
  if (typeof value !== "number" || Number.isNaN(value)) {
    return "Not available";
  }

  return `${value.toFixed(2)}%`;
}

export function humanizeCode(value?: string | null) {
  if (!value) {
    return "Unknown";
  }

  return value
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/[_-]/g, " ")
    .trim();
}

export function formatDifference(value?: number | null, currencyCode = "ZAR") {
  if (typeof value !== "number" || Number.isNaN(value)) {
    return "Not available";
  }

  const prefix = value > 0 ? "+" : "";
  return `${prefix}${formatCurrency(value, currencyCode)}`;
}
