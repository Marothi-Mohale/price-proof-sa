using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.Ocr;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Infrastructure.Options;
using PriceProof.Infrastructure.Storage;

namespace PriceProof.Infrastructure.Ocr;

public sealed class FileSystemReceiptDocumentContentResolver : IReceiptDocumentContentResolver
{
    private readonly DatabaseBinaryObjectStore _binaryObjectStore;

    public FileSystemReceiptDocumentContentResolver(
        IOptions<OcrOptions> _,
        DatabaseBinaryObjectStore binaryObjectStore)
    {
        _binaryObjectStore = binaryObjectStore;
    }

    public async Task<OcrDocumentContent> ResolveAsync(
        string fileName,
        string contentType,
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var file = await _binaryObjectStore.DownloadAsync(storagePath, cancellationToken);
            return new OcrDocumentContent(fileName, string.IsNullOrWhiteSpace(contentType) ? file.ContentType : contentType, file.Content);
        }
        catch (NotFoundException)
        {
            throw new ConflictException("The stored receipt file could not be accessed for OCR.");
        }
    }
}
