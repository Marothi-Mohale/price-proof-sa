using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.Ocr;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Infrastructure.Options;
using PriceProof.Infrastructure.Storage;

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

        try
        {
            return FileStoragePathResolver.Resolve(storagePath, _options.StorageRootPath);
        }
        catch (InvalidOperationException)
        {
            throw new ConflictException("The stored receipt file could not be accessed for OCR.");
        }
    }
}
