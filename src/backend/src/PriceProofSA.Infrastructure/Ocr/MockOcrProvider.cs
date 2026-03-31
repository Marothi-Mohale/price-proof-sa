using System.Text;
using System.Text.RegularExpressions;
using PriceProofSA.Application.Abstractions.Ocr;

namespace PriceProofSA.Infrastructure.Ocr;

public sealed partial class MockOcrProvider : IOcrProvider
{
    private readonly ReceiptTotalParser _parser;

    public MockOcrProvider(ReceiptTotalParser parser)
    {
        _parser = parser;
    }

    public string Name => "MockReceiptParser";

    public bool IsConfigured => true;

    public Task<OcrProviderResult> TryRecognizeAsync(OcrDocumentRequest request, CancellationToken cancellationToken = default)
    {
        string rawText;
        if (request.ContentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
        {
            rawText = Encoding.UTF8.GetString(request.Content);
        }
        else
        {
            rawText = request.FileName;
        }

        var parsedAmount = ExtractFromFilename(request.FileName) ?? _parser.Parse(rawText);
        var success = parsedAmount.HasValue || !string.IsNullOrWhiteSpace(rawText);
        var message = parsedAmount.HasValue
            ? "Mock OCR extracted a plausible amount."
            : "Mock OCR could not infer a total. Rename the file to include an amount like receipt-59.99.jpg for local demos.";

        return Task.FromResult(new OcrProviderResult(success, rawText, parsedAmount, message));
    }

    private static decimal? ExtractFromFilename(string fileName)
    {
        var match = FileAmountRegex().Match(fileName);
        if (!match.Success)
        {
            return null;
        }

        return decimal.TryParse(match.Value, out var value) ? value : null;
    }

    [GeneratedRegex(@"\d+(?:\.\d{2})")]
    private static partial Regex FileAmountRegex();
}
