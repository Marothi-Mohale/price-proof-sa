using System.Text;
using PriceProof.Application.Abstractions.Ocr;

namespace PriceProof.IntegrationTests.Fakes;

public sealed class FakeReceiptDocumentContentResolver : IReceiptDocumentContentResolver
{
    private const string ReceiptText = """
                                       SHOPRITE SANDTON CITY
                                       2026-04-01 12:45
                                       MILK 29.99
                                       BREAD 19.99
                                       TOTAL 49.98
                                       """;

    public Task<OcrDocumentContent> ResolveAsync(
        string fileName,
        string contentType,
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new OcrDocumentContent(
            fileName,
            string.IsNullOrWhiteSpace(contentType) ? "text/plain" : contentType,
            Encoding.UTF8.GetBytes(ReceiptText)));
    }
}
