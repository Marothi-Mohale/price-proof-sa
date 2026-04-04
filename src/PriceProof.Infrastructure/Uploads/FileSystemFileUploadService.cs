using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Application.Uploads;
using PriceProof.Infrastructure.Options;
using PriceProof.Infrastructure.Storage;

namespace PriceProof.Infrastructure.Uploads;

public sealed class FileSystemFileUploadService : IFileUploadService
{
    private static readonly Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider ContentTypeProvider = new();
    private static readonly IReadOnlyDictionary<string, string> ContentTypeAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpg"] = "image/jpeg",
            ["image/pjpeg"] = "image/jpeg",
            ["application/x-pdf"] = "application/pdf",
            ["audio/x-wav"] = "audio/wav",
            ["audio/wave"] = "audio/wav"
        };

    private readonly FileUploadOptions _options;
    private readonly DatabaseBinaryObjectStore _binaryObjectStore;

    public FileSystemFileUploadService(
        IOptions<FileUploadOptions> options,
        DatabaseBinaryObjectStore binaryObjectStore)
    {
        _options = options.Value;
        _binaryObjectStore = binaryObjectStore;
    }

    public async Task<UploadedFileDto> UploadAsync(IFormFile file, string category, Guid? caseId, CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            throw new BadRequestException("The uploaded file is empty.");
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            throw new BadRequestException($"The uploaded file exceeds the {_options.MaxFileSizeBytes / (1024 * 1024)} MB limit.");
        }

        var safeCategory = SanitizePathSegment(category, _options.MaxCategoryLength);
        var safeFileName = SanitizeFileName(file.FileName, _options.MaxFileNameLength);
        var contentType = ResolveContentType(safeFileName, file.ContentType);
        ValidateFileType(safeFileName, contentType);
        var caseSegment = caseId?.ToString("N") ?? "general";
        var storageFileName = $"{Guid.NewGuid():N}-{safeFileName}";
        var storageKey = _binaryObjectStore.CreateStorageKey("uploads", safeCategory, caseSegment, storageFileName);

        byte[] bytes;
        await using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream, cancellationToken);
            bytes = memoryStream.ToArray();
        }

        var contentHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        await _binaryObjectStore.SaveAsync(
            "uploads",
            storageKey,
            storageFileName,
            contentType,
            contentHash,
            bytes,
            caseId,
            cancellationToken);

        return new UploadedFileDto(
            storageFileName,
            contentType,
            storageKey,
            contentHash,
            bytes.LongLength);
    }

    public async Task<(byte[] Content, string ContentType, string FileName)> DownloadAsync(string storagePath, CancellationToken cancellationToken)
    {
        var file = await _binaryObjectStore.DownloadAsync(storagePath, cancellationToken);
        return (file.Content, file.ContentType, file.FileName);
    }

    private string ResolveContentType(string safeFileName, string? contentType)
    {
        var normalized = string.IsNullOrWhiteSpace(contentType)
            ? null
            : contentType.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalized) || normalized == "application/octet-stream")
        {
            return ContentTypeProvider.TryGetContentType(safeFileName, out var inferred)
                ? inferred.ToLowerInvariant()
                : "application/octet-stream";
        }

        var mediaType = normalized.Split(';', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];
        if (string.IsNullOrWhiteSpace(mediaType) || mediaType == "application/octet-stream")
        {
            return ContentTypeProvider.TryGetContentType(safeFileName, out var inferred)
                ? inferred.ToLowerInvariant()
                : "application/octet-stream";
        }

        return ContentTypeAliases.TryGetValue(mediaType, out var alias)
            ? alias
            : mediaType;
    }

    private void ValidateFileType(string safeFileName, string contentType)
    {
        var allowedExtensions = _options.AllowedExtensions
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.StartsWith('.') ? value.ToLowerInvariant() : $".{value.ToLowerInvariant()}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allowedContentTypes = _options.AllowedContentTypes
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var extension = Path.GetExtension(safeFileName);
        if (string.IsNullOrWhiteSpace(extension) || !allowedExtensions.Contains(extension))
        {
            throw new BadRequestException(
                $"The uploaded file type is not supported. Allowed formats: {string.Join(", ", allowedExtensions.OrderBy(value => value))}.");
        }

        if (!allowedContentTypes.Contains(contentType))
        {
            throw new BadRequestException(
                $"The uploaded file content type is not allowed. Allowed content types: {string.Join(", ", allowedContentTypes.OrderBy(value => value))}.");
        }
    }

    private static string SanitizePathSegment(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "general";
        }

        var normalized = new string(value
            .Trim()
            .ToLowerInvariant()
            .Where(character => !char.IsControl(character))
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .Take(Math.Max(8, maxLength))
            .ToArray())
            .Trim('-');

        return string.IsNullOrWhiteSpace(normalized) ? "general" : normalized;
    }

    private static string SanitizeFileName(string value, int maxLength)
    {
        var candidate = string.IsNullOrWhiteSpace(value) ? "upload.bin" : Path.GetFileName(value.Trim());
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(candidate
            .Where(character => !char.IsControl(character))
            .Select(character => invalidChars.Contains(character) ? '-' : character)
            .ToArray())
            .Trim();

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "upload.bin";
        }

        if (sanitized.Length <= maxLength)
        {
            return sanitized;
        }

        var extension = Path.GetExtension(sanitized);
        if (string.IsNullOrWhiteSpace(extension) || extension.Length >= maxLength)
        {
            return sanitized[..maxLength];
        }

        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
        var availableNameLength = Math.Max(1, maxLength - extension.Length);
        return $"{fileNameWithoutExtension[..Math.Min(fileNameWithoutExtension.Length, availableNameLength)]}{extension}";
    }
}
