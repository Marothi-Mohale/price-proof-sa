using PriceProof.Application.ComplaintPacks;

namespace PriceProof.Application.Abstractions.Services;

public interface IComplaintPackService
{
    Task<GeneratedComplaintPackDto> GenerateAsync(Guid caseId, CancellationToken cancellationToken);

    Task<ComplaintPackDownloadDto> DownloadAsync(Guid complaintPackId, CancellationToken cancellationToken);
}
