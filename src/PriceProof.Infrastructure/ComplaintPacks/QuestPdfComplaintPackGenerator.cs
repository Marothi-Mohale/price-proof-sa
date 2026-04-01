using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.ComplaintPacks;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Infrastructure.Options;
using PriceProof.Infrastructure.Storage;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PriceProof.Infrastructure.ComplaintPacks;

public sealed class QuestPdfComplaintPackGenerator : IComplaintPackGenerator
{
    private const string AccentColor = "#14532D";
    private const string AccentSoftColor = "#E8F5EC";
    private const string BorderColor = "#D6D3D1";
    private const string MutedColor = "#57534E";

    private readonly ComplaintPackOptions _options;
    private readonly ILogger<QuestPdfComplaintPackGenerator> _logger;

    static QuestPdfComplaintPackGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public QuestPdfComplaintPackGenerator(
        IOptions<ComplaintPackOptions> options,
        ILogger<QuestPdfComplaintPackGenerator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<GeneratedComplaintPackDocument> GenerateAsync(ComplaintPackBuildRequest request, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            throw new ConflictException("Complaint pack generation is currently disabled.");
        }

        try
        {
            var content = Document
                .Create(document => ComposeDocument(document, request))
                .WithSettings(new DocumentSettings
                {
                    CompressDocument = true,
                    ImageCompressionQuality = ImageCompressionQuality.High,
                    ContentDirection = ContentDirection.LeftToRight
                })
                .GeneratePdf();

            return Task.FromResult(new GeneratedComplaintPackDocument(
                BuildFileName(request),
                "application/pdf",
                content));
        }
        catch (Exception exception) when (exception is not ConflictException)
        {
            _logger.LogError(exception, "Failed to generate complaint pack PDF for case {CaseReferenceNumber}", request.CaseReferenceNumber);
            throw new ServiceUnavailableException("Complaint pack generation is currently unavailable. Please try again later.", exception);
        }
    }

    private void ComposeDocument(IDocumentContainer document, ComplaintPackBuildRequest request)
    {
        document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(TextStyle.Default.FontSize(10).FontColor(Colors.Black).LineHeight(1.25f));

            page.Header().Element(container => ComposeHeader(container, request));

            page.Content().PaddingTop(12).Column(column =>
            {
                column.Spacing(14);
                column.Item().Element(container => ComposeCaseOverview(container, request));
                column.Item().Element(container => ComposeStructuredSummary(container, request));
                column.Item().Element(container => ComposeTimeline(container, request));
                column.Item().Element(container => ComposeEvidenceInventory(container, request));
                column.Item().Element(container => ComposeDeclaration(container, request));
            });

            page.Footer().PaddingTop(8).Row(row =>
            {
                row.RelativeItem()
                    .DefaultTextStyle(x => x.FontSize(8).FontColor(MutedColor))
                    .Text(text =>
                    {
                        text.Span("Audit timestamp: ").SemiBold();
                        text.Span($"{request.AuditTimestampUtc:yyyy-MM-dd HH:mm:ss 'UTC'}");
                    });

                row.ConstantItem(100)
                    .AlignRight()
                    .DefaultTextStyle(x => x.FontSize(8).FontColor(MutedColor))
                    .Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
            });
        });
    }

    private void ComposeHeader(IContainer container, ComplaintPackBuildRequest request)
    {
        container.Row(row =>
        {
            row.ConstantItem(64).Height(64).Element(ComposeLogo);

            row.RelativeItem().PaddingLeft(14).Column(column =>
            {
                column.Item().Text(_options.Title)
                    .FontSize(20)
                    .Bold()
                    .FontColor(AccentColor);

                column.Item().Text("Evidence pack for submission to a bank, merchant, or consumer complaint channel.")
                    .FontSize(10)
                    .FontColor(MutedColor);

                column.Item()
                    .PaddingTop(6)
                    .DefaultTextStyle(x => x.FontSize(9))
                    .Text(text =>
                    {
                        text.Span("Case reference: ").SemiBold();
                        text.Span(request.CaseReferenceNumber);
                        text.Span("  |  Generated: ").SemiBold();
                        text.Span($"{request.AuditTimestampUtc:dd MMM yyyy HH:mm 'UTC'}");
                    });
            });
        });
    }

    private void ComposeLogo(IContainer container)
    {
        container
            .Background(AccentSoftColor)
            .Border(1)
            .BorderColor(AccentColor)
            .CornerRadius(10)
            .AlignCenter()
            .AlignMiddle()
            .Text(_options.LogoText)
            .FontSize(26)
            .Bold()
            .FontColor(AccentColor);
    }

    private void ComposeCaseOverview(IContainer container, ComplaintPackBuildRequest request)
    {
        SectionCard(container, "Case overview", body =>
        {
            body.Spacing(10);

            body.Item().Row(row =>
            {
                row.Spacing(8);
                row.RelativeItem().Element(cell => ComposeMetricCard(cell, "Quoted / displayed price", FormatMoney(request.CurrencyCode, request.QuotedAmount)));
                row.RelativeItem().Element(cell => ComposeMetricCard(cell, "Charged amount", FormatMoney(request.CurrencyCode, request.ChargedAmount)));
            });

            body.Item().Row(row =>
            {
                row.Spacing(8);
                row.RelativeItem().Element(cell => ComposeMetricCard(cell, "Discrepancy amount", FormatMoney(request.CurrencyCode, request.DifferenceAmount)));
                row.RelativeItem().Element(cell => ComposeMetricCard(cell, "Percentage difference", request.PercentageDifference.HasValue ? $"{request.PercentageDifference.Value:0.00}%" : "Not available"));
            });

            body.Item().LineHorizontal(1).LineColor(BorderColor);

            body.Item().Row(row =>
            {
                row.Spacing(12);

                row.RelativeItem().Column(column =>
                {
                    column.Spacing(4);
                    column.Item().Text("Store and branch details").SemiBold().FontColor(AccentColor);
                    column.Item().Text(request.MerchantName).Bold();

                    if (!string.IsNullOrWhiteSpace(request.BranchName))
                    {
                        column.Item().Text($"Branch: {request.BranchName}");
                    }

                    if (!string.IsNullOrWhiteSpace(request.BranchCode))
                    {
                        column.Item().Text($"Branch code: {request.BranchCode}");
                    }

                    foreach (var line in BuildAddressLines(request))
                    {
                        column.Item().Text(line);
                    }

                    if (!string.IsNullOrWhiteSpace(request.MerchantWebsiteUrl))
                    {
                        column.Item().Text(text =>
                        {
                            text.Span("Website: ").SemiBold();
                            text.Hyperlink(request.MerchantWebsiteUrl!, request.MerchantWebsiteUrl!).FontColor(Colors.Blue.Darken2).Underline();
                        });
                    }
                });

                row.RelativeItem().Column(column =>
                {
                    column.Spacing(4);
                    column.Item().Text("Case context").SemiBold().FontColor(AccentColor);
                    column.Item().Text($"Basket: {request.BasketDescription}");
                    column.Item().Text($"Incident: {request.IncidentAtUtc:dd MMM yyyy HH:mm 'UTC'}");
                    column.Item().Text($"Classification: {request.ClassificationLabel}");

                    if (request.Confidence.HasValue)
                    {
                        column.Item().Text($"Confidence: {request.Confidence.Value:P0}");
                    }

                    column.Item().Text($"Reported by: {request.ReportedByDisplayName} ({request.ReportedByEmail})");
                });
            });
        });
    }

    private void ComposeStructuredSummary(IContainer container, ComplaintPackBuildRequest request)
    {
        SectionCard(container, "Complaint summary", body =>
        {
            body.Spacing(10);
            body.Item().Text(request.ComplaintSummary);

            body.Item().Background(AccentSoftColor).Padding(10).Border(1).BorderColor(BorderColor).CornerRadius(6).Column(column =>
            {
                column.Spacing(4);
                column.Item().Text("Classification and explanation").SemiBold().FontColor(AccentColor);
                column.Item().Text($"Current classification: {request.ClassificationLabel}");
                column.Item().Text(request.Explanation);
                column.Item().Text($"Evidence strength: {request.EvidenceStrength}").SemiBold();
                column.Item().Text(request.EvidenceStrengthExplanation);
            });
        });
    }

    private void ComposeTimeline(IContainer container, ComplaintPackBuildRequest request)
    {
        SectionCard(container, "Timeline of events", body =>
        {
            body.Spacing(6);

            foreach (var item in request.Timeline.OrderBy(entry => entry.OccurredAtUtc))
            {
                body.Item().BorderLeft(3).BorderColor(AccentColor).PaddingLeft(10).Column(column =>
                {
                    column.Spacing(2);
                    column.Item().Text($"{item.OccurredAtUtc:dd MMM yyyy HH:mm 'UTC'}  |  {item.Title}")
                        .SemiBold()
                        .FontColor(AccentColor);
                    column.Item().Text(item.Description).FontColor(MutedColor);
                });
            }
        });
    }

    private void ComposeEvidenceInventory(IContainer container, ComplaintPackBuildRequest request)
    {
        SectionCard(container, "Evidence inventory", body =>
        {
            body.Spacing(12);

            if (request.EvidenceInventory.Count == 0)
            {
                body.Item().Text("No uploaded evidence is currently attached to this case.");
                return;
            }

            foreach (var evidence in request.EvidenceInventory)
            {
                body.Item().Border(1).BorderColor(BorderColor).CornerRadius(8).Padding(10).Row(row =>
                {
                    if (_options.IncludeEvidencePreviews)
                    {
                        row.ConstantItem(120).Height(90).Element(preview => ComposeEvidencePreview(preview, evidence));
                        row.RelativeItem().PaddingLeft(10).Element(details => ComposeEvidenceDetails(details, evidence));
                    }
                    else
                    {
                        row.RelativeItem().Element(details => ComposeEvidenceDetails(details, evidence));
                    }
                });
            }
        });
    }

    private void ComposeDeclaration(IContainer container, ComplaintPackBuildRequest request)
    {
        SectionCard(container, "User declaration", body =>
        {
            body.Spacing(8);
            body.Item().Text(request.DeclarationText);
            body.Item().PaddingTop(12).Text($"Declared by: {request.ReportedByDisplayName}");
            body.Item().Text($"Declaration timestamp: {request.AuditTimestampUtc:yyyy-MM-dd HH:mm:ss 'UTC'}");
        });
    }

    private void ComposeMetricCard(IContainer container, string label, string value)
    {
        container
            .Border(1)
            .BorderColor(BorderColor)
            .Background(Colors.Grey.Lighten5)
            .CornerRadius(8)
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(4);
                column.Item().Text(label).FontSize(9).FontColor(MutedColor);
                column.Item().Text(value).FontSize(15).Bold().FontColor(AccentColor);
            });
    }

    private void ComposeEvidencePreview(IContainer container, ComplaintPackEvidenceEntry evidence)
    {
        container.Border(1).BorderColor(BorderColor).CornerRadius(6).Padding(4).Element(inner =>
        {
            var imageBytes = TryLoadPreviewBytes(evidence);

            if (imageBytes is not null)
            {
                inner.Image(imageBytes).FitArea();
                return;
            }

            inner.Background(Colors.Grey.Lighten4).AlignCenter().AlignMiddle().Column(column =>
            {
                column.Spacing(4);
                column.Item().AlignCenter().Text("Preview unavailable").FontSize(9).SemiBold().FontColor(MutedColor);
                column.Item().AlignCenter().Text(evidence.ContentType ?? "Unsupported type").FontSize(8).FontColor(MutedColor);
            });
        });
    }

    private void ComposeEvidenceDetails(IContainer container, ComplaintPackEvidenceEntry evidence)
    {
        container.Column(column =>
        {
            column.Spacing(4);
            column.Item().Text($"{evidence.Label} [{evidence.Category}]").SemiBold().FontColor(AccentColor);
            column.Item().Text($"File: {evidence.FileName}");
            column.Item().Text($"Recorded: {evidence.RecordedAtUtc:dd MMM yyyy HH:mm 'UTC'}");

            if (evidence.Amount.HasValue)
            {
                column.Item().Text($"Associated amount: {FormatMoney(evidence.CurrencyCode, evidence.Amount.Value)}");
            }

            if (_options.IncludeEvidenceReferences)
            {
                if (!string.IsNullOrWhiteSpace(evidence.ReferenceLink))
                {
                    column.Item().Text(text =>
                    {
                        text.Span("Reference: ").SemiBold();
                        text.Hyperlink(evidence.ReferenceLink!, evidence.ReferenceLink!).FontColor(Colors.Blue.Darken2).Underline();
                    });
                }
                else
                {
                    column.Item().Text($"Storage reference: {evidence.StoragePath}");
                }
            }

            if (!string.IsNullOrWhiteSpace(evidence.Notes))
            {
                column.Item().Text($"Notes: {evidence.Notes}");
            }
        });
    }

    private byte[]? TryLoadPreviewBytes(ComplaintPackEvidenceEntry evidence)
    {
        if (string.IsNullOrWhiteSpace(evidence.ContentType) || !evidence.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!FileStoragePathResolver.TryResolve(evidence.StoragePath, _options.StorageRootPath, out var resolvedPath) ||
            string.IsNullOrWhiteSpace(resolvedPath) ||
            !File.Exists(resolvedPath))
        {
            return null;
        }

        try
        {
            return File.ReadAllBytes(resolvedPath);
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "Could not load evidence preview from {StoragePath}", evidence.StoragePath);
            return null;
        }
        catch (UnauthorizedAccessException exception)
        {
            _logger.LogWarning(exception, "Could not load evidence preview from {StoragePath}", evidence.StoragePath);
            return null;
        }
    }

    private static IEnumerable<string> BuildAddressLines(ComplaintPackBuildRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.BranchAddressLine1))
        {
            yield return request.BranchAddressLine1!;
        }

        if (!string.IsNullOrWhiteSpace(request.BranchAddressLine2))
        {
            yield return request.BranchAddressLine2!;
        }

        var cityLine = string.Join(", ", new[] { request.BranchCity, request.BranchProvince, request.BranchPostalCode }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim()));

        if (!string.IsNullOrWhiteSpace(cityLine))
        {
            yield return cityLine;
        }
    }

    private static string BuildFileName(ComplaintPackBuildRequest request)
    {
        var merchantSlug = new string(request.MerchantName
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray())
            .Trim('-');

        return $"{request.CaseReferenceNumber.ToLowerInvariant()}-{merchantSlug}-complaint-pack.pdf";
    }

    private static string FormatMoney(string currencyCode, decimal amount)
    {
        return $"{currencyCode.ToUpperInvariant()} {amount:0.00}";
    }

    private static void SectionCard(IContainer container, string heading, Action<ColumnDescriptor> composeBody)
    {
        container
            .Border(1)
            .BorderColor(BorderColor)
            .CornerRadius(10)
            .Padding(14)
            .Column(column =>
            {
                column.Spacing(10);
                column.Item().Text(heading).FontSize(13).Bold().FontColor(AccentColor);
                column.Item().Element(inner => inner.Column(composeBody));
            });
    }
}
