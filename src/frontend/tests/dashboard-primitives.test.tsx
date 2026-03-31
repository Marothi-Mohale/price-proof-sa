import { render, screen } from "@testing-library/react";
import { EmptyState, StatusPill } from "@/components/dashboard-primitives";

describe("dashboard primitives", () => {
  it("renders empty state copy", () => {
    render(<EmptyState title="Nothing here" body="Start by creating a case." />);

    expect(screen.getByText("Nothing here")).toBeInTheDocument();
    expect(screen.getByText("Start by creating a case.")).toBeInTheDocument();
  });

  it("renders flagged status pill", () => {
    render(<StatusPill label="LikelyCardSurcharge" flagged />);

    expect(screen.getByText("LikelyCardSurcharge")).toBeInTheDocument();
  });
});
