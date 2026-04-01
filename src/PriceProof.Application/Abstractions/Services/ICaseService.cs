using PriceProof.Application.Cases;
using PriceProof.Application.Common.Models;

namespace PriceProof.Application.Abstractions.Services;

public interface ICaseService
{
    Task<CaseDetailDto> CreateAsync(CreateCaseRequest request, CancellationToken cancellationToken);

    Task<CaseDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<CaseSummaryDto>> ListAsync(GetCasesQuery query, CancellationToken cancellationToken);

    Task<CaseAnalysisDto> AnalyzeAsync(Guid id, AnalyzeCaseRequest request, CancellationToken cancellationToken);
}
