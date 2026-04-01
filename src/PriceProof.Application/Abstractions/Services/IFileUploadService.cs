using Microsoft.AspNetCore.Http;
using PriceProof.Application.Uploads;

namespace PriceProof.Application.Abstractions.Services;

public interface IFileUploadService
{
    Task<UploadedFileDto> UploadAsync(IFormFile file, string category, Guid? caseId, CancellationToken cancellationToken);

    Task<(byte[] Content, string ContentType, string FileName)> DownloadAsync(string storagePath, CancellationToken cancellationToken);
}
