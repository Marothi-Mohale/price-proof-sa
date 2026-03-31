namespace PriceProofSA.Application.Abstractions.Complaints;

public sealed record GeneratedDocument(
    string FileName,
    string ContentType,
    byte[] Content,
    string Summary);
