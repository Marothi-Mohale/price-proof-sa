using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.ComplaintPacks;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Infrastructure.Options;
using PriceProof.Infrastructure.Storage;

namespace PriceProof.Infrastructure.ComplaintPacks;

public sealed class FileSystemComplaintPackDocumentStore : IComplaintPackDocumentStore
{
    private readonly ComplaintPackOptions _options;

    public FileSystemComplaintPackDocumentStore(IOptions<ComplaintPackOptions> options)
    {
        _options = options.Value;
    }

    public async Task<StoredComplaintPackDocument> SaveAsync(
        Guid caseId,
        GeneratedComplaintPackDocument document,
        CancellationToken cancellationToken)
    {
        var safeFileName = SanitizeFileName(document.FileName);
        var root = GetResolvedRootPath();
        var relativeDirectory = Path.Combine("complaint-packs", caseId.ToString("N"));
        var absoluteDirectory = Path.Combine(root, relativeDirectory);

        Directory.CreateDirectory(absoluteDirectory);

        var absolutePath = Path.Combine(absoluteDirectory, safeFileName);
        await File.WriteAllBytesAsync(absolutePath, document.Content, cancellationToken);

        var relativePath = Path.Combine(relativeDirectory, safeFileName)
            .Replace(Path.DirectorySeparatorChar, '/');
        var hash = Convert.ToHexString(SHA256.HashData(document.Content)).ToLowerInvariant();

        return new StoredComplaintPackDocument(
            safeFileName,
            relativePath,
            hash,
            document.Content.LongLength);
    }

    public async Task<ComplaintPackDownloadFile> DownloadAsync(string fileName, string storagePath, CancellationToken cancellationToken)
    {
        var resolvedPath = FileStoragePathResolver.Resolve(storagePath, _options.StorageRootPath);

        if (!File.Exists(resolvedPath))
        {
            throw new ServiceUnavailableException("The complaint pack file is currently unavailable. Please generate it again if the problem continues.");
        }

        try
        {
            var content = await File.ReadAllBytesAsync(resolvedPath, cancellationToken);
            return new ComplaintPackDownloadFile(fileName, "application/pdf", content);
        }
        catch (IOException)
        {
            throw new ServiceUnavailableException("The complaint pack file is currently unavailable. Please generate it again if the problem continues.");
        }
        catch (UnauthorizedAccessException)
        {
            throw new ServiceUnavailableException("The complaint pack file is currently unavailable. Please generate it again if the problem continues.");
        }
    }

    private string GetResolvedRootPath()
    {
        return Path.IsPathRooted(_options.StorageRootPath)
            ? _options.StorageRootPath
            : Path.Combine(Directory.GetCurrentDirectory(), _options.StorageRootPath);
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
