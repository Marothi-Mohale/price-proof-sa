using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.Ocr;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Infrastructure.Options;

namespace PriceProof.Infrastructure.Ocr;

public sealed class FileSystemReceiptDocumentContentResolver : IReceiptDocumentContentResolver
{
    private readonly OcrOptions _options;

    public FileSystemReceiptDocumentContentResolver(IOptions<OcrOptions> options)
    {
        _options = options.Value;
    }

    public async Task<OcrDocumentContent> ResolveAsync(
        string fileName,
        string contentType,
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        var resolvedPath = ResolvePath(storagePath);

        if (!File.Exists(resolvedPath))
        {
            throw new ConflictException("The stored receipt file could not be accessed for OCR.");
        }

        try
        {
            var content = await File.ReadAllBytesAsync(resolvedPath, cancellationToken);
            return new OcrDocumentContent(fileName, contentType, content);
        }
        catch (IOException)
        {
            throw new ConflictException("The stored receipt file could not be accessed for OCR.");
        }
        catch (UnauthorizedAccessException)
        {
            throw new ConflictException("The stored receipt file could not be accessed for OCR.");
        }
    }

    private string ResolvePath(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new ConflictException("The stored receipt file could not be accessed for OCR.");
        }

        if (Uri.TryCreate(storagePath, UriKind.Absolute, out var absoluteUri))
        {
            if (!absoluteUri.IsFile)
            {
                throw new ConflictException("The stored receipt file could not be accessed for OCR.");
            }

            return absoluteUri.LocalPath;
        }

        if (Path.IsPathFullyQualified(storagePath))
        {
            return storagePath;
        }

        var relativePath = storagePath
            .TrimStart('/', '\\')
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        var root = Path.IsPathRooted(_options.StorageRootPath)
            ? _options.StorageRootPath
            : Path.Combine(Directory.GetCurrentDirectory(), _options.StorageRootPath);

        return Path.GetFullPath(Path.Combine(root, relativePath));
    }
}
