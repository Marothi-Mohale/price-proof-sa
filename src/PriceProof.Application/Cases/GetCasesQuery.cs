using PriceProof.Domain.Enums;

namespace PriceProof.Application.Cases;

public sealed record GetCasesQuery(
    Guid? MerchantId = null,
    Guid? ReportedByUserId = null,
    CaseClassification? Classification = null,
    int Skip = 0,
    int Take = 25);
