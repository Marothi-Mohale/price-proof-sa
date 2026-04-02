using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.ComplaintPacks;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Infrastructure.Options;
using PriceProof.Infrastructure.Storage;

namespace PriceProof.Infrastructure.ComplaintPacks;

public sealed class FileSystemComplaintPackDocumentStore : IComplaintPackDocumentStore
{
    private readonly DatabaseBinaryObjectStore _binaryObjectStore;

    public FileSystemComplaintPackDocumentStore(
        IOptions<ComplaintPackOptions> _,
        DatabaseBinaryObjectStore binaryObjectStore)
    {
        _binaryObjectStore = binaryObjectStore;
    }

    public async Task<StoredComplaintPackDocument> SaveAsync(
        Guid caseId,
        GeneratedComplaintPackDocument document,
        CancellationToken cancellationToken)
    {
        var safeFileName = SanitizeFileName(document.FileName);
        var storageKey = _binaryObjectStore.CreateStorageKey("complaint-packs", caseId.ToString("N"), safeFileName);
        var hash = Convert.ToHexString(SHA256.HashData(document.Content)).ToLowerInvariant();
        await _binaryObjectStore.SaveAsync(
            "complaint-packs",
            storageKey,
            safeFileName,
            document.ContentType,
            hash,
            document.Content,
            caseId,
            cancellationToken);

        return new StoredComplaintPackDocument(
            safeFileName,
            storageKey,
            hash,
            document.Content.LongLength);
    }

    public async Task<ComplaintPackDownloadFile> DownloadAsync(string fileName, string storagePath, CancellationToken cancellationToken)
    {
        try
        {
            var file = await _binaryObjectStore.DownloadAsync(storagePath, cancellationToken);
            return new ComplaintPackDownloadFile(file.FileName, file.ContentType, file.Content);
        }
        catch (NotFoundException)
        {
            throw new ServiceUnavailableException("The complaint pack file is currently unavailable. Please generate it again if the problem continues.");
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return $"priceproof-complaint-pack-{Guid.NewGuid():N}.pdf";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Trim().Select(character => invalidChars.Contains(character) ? '-' : character).ToArray());

        if (!sanitized.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            sanitized += ".pdf";
        }

        return sanitized;
    }
}
