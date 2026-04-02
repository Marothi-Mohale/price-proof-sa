namespace PriceProof.Application.Admin;

public class AdminDashboardFilterQuery
{
    public DateOnly? FromDate { get; init; }

    public DateOnly? ToDate { get; init; }

    public string? Province { get; init; }

    public string? City { get; init; }
}

public sealed class AdminDashboardTableQuery : AdminDashboardFilterQuery
{
    public int Skip { get; init; }

    public int Take { get; init; } = 10;
}
