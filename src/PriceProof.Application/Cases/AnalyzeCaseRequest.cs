using FluentValidation;

namespace PriceProof.Application.Cases;

public sealed record AnalyzeCaseRequest(
    bool MerchantSaidCardFee = false,
    bool CashbackPresent = false,
    bool DeliveryOrServiceFeePresent = false,
    string? EvidenceText = null);

public sealed class AnalyzeCaseRequestValidator : AbstractValidator<AnalyzeCaseRequest>
{
    public AnalyzeCaseRequestValidator()
    {
        RuleFor(request => request.EvidenceText).MaximumLength(4000);
    }
}
