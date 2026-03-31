using PriceProofSA.Domain.Common;
using PriceProofSA.Domain.Enums;

namespace PriceProofSA.Domain.Entities;

public sealed class ReceiptRecord : BaseEntity
{
    private ReceiptRecord()
    {
    }

    public Guid PaymentRecordId { get; private set; }

    public PaymentRecord? PaymentRecord { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public string StoragePath { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public string ContentHash { get; private set; } = string.Empty;

    public OcrProcessingStatus OcrStatus { get; private set; }

    public string? OcrRawText { get; private set; }

    public decimal? ParsedTotalAmount { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public int RetryCount { get; private set; }

    public static ReceiptRecord Create(
        Guid paymentRecordId,
        string fileName,
        string contentType,
        string storagePath,
        long sizeBytes,
        string contentHash)
    {
        return new ReceiptRecord
        {
            PaymentRecordId = paymentRecordId,
            FileName = fileName,
            ContentType = contentType,
            StoragePath = storagePath,
            SizeBytes = sizeBytes,
            ContentHash = contentHash,
            OcrStatus = OcrProcessingStatus.Pending
        };
    }

    public void CompleteOcr(string? rawText, decimal? parsedTotalAmount, DateTimeOffset processedAtUtc)
    {
        OcrRawText = rawText;
        ParsedTotalAmount = parsedTotalAmount;
        ProcessedAtUtc = processedAtUtc;
        OcrStatus = OcrProcessingStatus.Completed;
    }

    public void MarkFailed(DateTimeOffset processedAtUtc, string? rawText = null)
    {
        RetryCount += 1;
        OcrRawText = rawText;
        ProcessedAtUtc = processedAtUtc;
        OcrStatus = OcrProcessingStatus.Failed;
    }

    public void MarkNoProvider(DateTimeOffset processedAtUtc)
    {
        RetryCount += 1;
        ProcessedAtUtc = processedAtUtc;
        OcrStatus = OcrProcessingStatus.NoProviderConfigured;
    }
}
