import { formatCurrency, humanizeCode } from "@/lib/format";

describe("format helpers", () => {
  it("formats currency values", () => {
    const formatted = formatCurrency(12.5, "ZAR");

    expect(formatted).toContain("12");
    expect(formatted).toContain("50");
  });

  it("humanizes backend enum codes", () => {
    expect(humanizeCode("LikelyCardSurcharge")).toBe("Likely Card Surcharge");
  });
});
