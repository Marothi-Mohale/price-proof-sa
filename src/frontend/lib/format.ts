const moneyFormatter = new Intl.NumberFormat("en-ZA", {
  style: "currency",
  currency: "ZAR",
  minimumFractionDigits: 2
});

const dateFormatter = new Intl.DateTimeFormat("en-ZA", {
  dateStyle: "medium",
  timeStyle: "short"
});

export function formatMoney(amount: number | null | undefined) {
  return typeof amount === "number" ? moneyFormatter.format(amount) : "Not captured";
}

export function formatDate(value: string | null | undefined) {
  return value ? dateFormatter.format(new Date(value)) : "Pending";
}
