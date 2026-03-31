using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PriceProofSA.Application.Abstractions.Ocr;
using PriceProofSA.Application.Services;
using PriceProofSA.Infrastructure.Persistence;
using PriceProofSA.Infrastructure.Storage;

namespace PriceProofSA.Infrastructure.BackgroundJobs;

public sealed class ReceiptOcrJob
{
    private readonly PriceProofDbContext _dbContext;
    private readonly RoutedFileStorageService _fileStorageService;
    private readonly IOcrOrchestrator _ocrOrchestrator;
    private readonly CaseService _caseService;
    private readonly ILogger<ReceiptOcrJob> _logger;

    public ReceiptOcrJob(
        PriceProofDbContext dbContext,
        RoutedFileStorageService fileStorageService,
        IOcrOrchestrator ocrOrchestrator,
        CaseService caseService,
        ILogger<ReceiptOcrJob> logger)
    {
        _dbContext = dbContext;
        _fileStorageService = fileStorageService;
        _ocrOrchestrator = ocrOrchestrator;
        _caseService = caseService;
        _logger = logger;
    }

    public async Task ProcessAsync(Guid receiptId)
    {
        var receipt = await _dbContext.ReceiptRecords
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == receiptId);

        if (receipt is null)
        {
            _logger.LogWarning("Receipt OCR job could not find receipt {ReceiptId}", receiptId);
            return;
        }

        try
        {
            var file = await _fileStorageService.OpenReadAsync(receipt.StoragePath);
            await using var fileContent = file.Content;
            await using var memory = new MemoryStream();
            await fileContent.CopyToAsync(memory);

            var result = await _ocrOrchestrator.RecognizeReceiptAsync(
                new OcrDocumentRequest(receipt.FileName, receipt.ContentType, memory.ToArray()));

            await _caseService.ApplyReceiptOcrResultAsync(receipt.Id, result);
            _logger.LogInformation("Processed OCR for receipt {ReceiptId} using {Provider}", receiptId, result.ProviderName);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Receipt OCR job failed for receipt {ReceiptId}", receiptId);
            await _caseService.ApplyReceiptOcrResultAsync(
                receipt.Id,
                new OcrDocumentResult(false, "BackgroundJob", string.Empty, null, exception.Message));
        }
    }
}
