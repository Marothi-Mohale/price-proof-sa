namespace PriceProofSA.Application.Abstractions.Complaints;

public interface IComplaintPackGenerator
{
    Task<GeneratedDocument> GenerateAsync(ComplaintPackBuildRequest request, CancellationToken cancellationToken = default);
}
