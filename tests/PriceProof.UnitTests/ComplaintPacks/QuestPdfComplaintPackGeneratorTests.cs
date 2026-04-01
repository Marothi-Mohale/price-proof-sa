using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.ComplaintPacks;
using PriceProof.Infrastructure.ComplaintPacks;
using PriceProof.Infrastructure.Options;
using QuestPDF.Infrastructure;

namespace PriceProof.UnitTests.ComplaintPacks;

public sealed class QuestPdfComplaintPackGeneratorTests
{
    private static readonly byte[] TinyPngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9WlH0n0AAAAASUVORK5CYII=");

    [Fact]
    public async Task Should_generate_pdf_for_request_with_local_image_preview()
    {
        var storageRootPath = Path.Combine(Path.GetTempPath(), "priceproof-generator-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(storageRootPath, "evidence", "prices"));
        await File.WriteAllBytesAsync(Path.Combine(storageRootPath, "evidence", "prices", "display.png"), TinyPngBytes);

        var options = Options.Create(new ComplaintPackOptions
        {
            Enabled = true,
            StorageRootPath = storageRootPath,
            IncludeEvidencePreviews = true,
            IncludeEvidenceReferences = false
        });

        try
        {
            QuestPDF.Settings.EnableDebugging = true;
            var generator = new QuestPdfComplaintPackGenerator(options, NullLogger<QuestPdfComplaintPackGenerator>.Instance);

            var exception = await Record.ExceptionAsync(() => generator.GenerateAsync(
                new ComplaintPackBuildRequest(
                    "PP-20260401-PDF",
                    "Shoprite",
                    "Groceries",
                    "https://www.shoprite.co.za",
                    "Sandton City",
                    "JHB-SANDTON",
                    "Sandton City, Rivonia Road",
                    null,
                    "Johannesburg",
                    "Gauteng",
                    "2196",
                    "Demo Investigator",
                    "investigator@priceproof.local",
                    "Milk and bread",
                    new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero),
                    "ZAR",
                    99.99m,
                    109.99m,
                    10.00m,
                    10.00m,
                    "LikelyCardSurcharge",
                    "Likely Card Surcharge",
                    0.94m,
                    "The recorded evidence suggests a card fee explanation.",
                    "Strong",
                    "This pack includes both quoted and receipt-backed evidence.",
                    [
                        new ComplaintPackTimelineEntry(
                            new DateTimeOffset(2026, 4, 1, 10, 0, 0, TimeSpan.Zero),
                            "Incident recorded",
                            "The discrepancy was reported after checkout.")
                    ],
                    [
                        new ComplaintPackEvidenceEntry(
                            "QuotedPriceEvidence",
                            "Price Tag Photo",
                            "display.png",
                            "image/png",
                            "evidence/prices/display.png",
                            new DateTimeOffset(2026, 4, 1, 9, 55, 0, TimeSpan.Zero),
                            null,
                            "ZAR",
                            99.99m,
                            "Displayed price on shelf.")
                    ],
                    "This complaint pack relates to a recorded pricing discrepancy.",
                    "I confirm that this complaint pack reflects the records and supporting material I submitted to PriceProof SA, to the best of my knowledge.",
                    new DateTimeOffset(2026, 4, 1, 10, 5, 0, TimeSpan.Zero)),
                CancellationToken.None));

            exception.Should().BeNull(exception?.ToString());
        }
        finally
        {
            QuestPDF.Settings.EnableDebugging = false;

            if (Directory.Exists(storageRootPath))
            {
                Directory.Delete(storageRootPath, recursive: true);
            }
        }
    }
}
