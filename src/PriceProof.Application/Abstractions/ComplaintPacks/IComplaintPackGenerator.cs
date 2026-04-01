namespace PriceProof.Application.Abstractions.ComplaintPacks;

public interface IComplaintPackGenerator
{
    Task<GeneratedComplaintPackDocument> GenerateAsync(ComplaintPackBuildRequest request, CancellationToken cancellationToken);
}
