namespace PriceProof.Application.Abstractions.ComplaintPacks;

public interface IComplaintPackDocumentStore
{
    Task<StoredComplaintPackDocument> SaveAsync(Guid caseId, GeneratedComplaintPackDocument document, CancellationToken cancellationToken);

    Task<ComplaintPackDownloadFile> DownloadAsync(string fileName, string storagePath, CancellationToken cancellationToken);
}
