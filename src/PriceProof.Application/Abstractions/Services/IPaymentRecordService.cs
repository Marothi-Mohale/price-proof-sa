using PriceProof.Application.PaymentRecords;

namespace PriceProof.Application.Abstractions.Services;

public interface IPaymentRecordService
{
    Task<PaymentRecordDto> CreateAsync(CreatePaymentRecordRequest request, CancellationToken cancellationToken);
}
