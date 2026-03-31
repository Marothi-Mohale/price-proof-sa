import { formatDate, formatMoney } from "@/lib/format";

describe("format helpers", () => {
  it("formats rand values", () => {
    expect(formatMoney(12.5)).toContain("R");
  });

  it("returns pending for missing dates", () => {
    expect(formatDate(null)).toBe("Pending");
  });
});
