namespace PriceProof.Application.Uploads;

public sealed record UploadedFileDto(
    string FileName,
    string ContentType,
    string StoragePath,
    string ContentHash,
    long SizeBytes);
