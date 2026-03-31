using PriceProof.Application.ReceiptRecords;

namespace PriceProof.Application.Abstractions.Services;

public interface IReceiptRecordService
{
    Task<ReceiptRecordDto> CreateAsync(CreateReceiptRecordRequest request, CancellationToken cancellationToken);
}
