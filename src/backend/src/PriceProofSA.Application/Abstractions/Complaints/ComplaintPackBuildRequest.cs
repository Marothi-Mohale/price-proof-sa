namespace PriceProofSA.Application.Abstractions.Complaints;

public sealed record ComplaintPackBuildRequest(
    Guid CaseId,
    string StoreName,
    string BasketDescription,
    decimal QuotedAmount,
    decimal ChargedAmount,
    decimal DifferenceAmount,
    string Classification,
    string Summary,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastUpdatedAtUtc,
    IReadOnlyCollection<ComplaintAttachment> Attachments);

public sealed record ComplaintAttachment(
    string Label,
    string FileName,
    string ContentType,
    string StoragePath,
    DateTimeOffset CapturedAtUtc);
