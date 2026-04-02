using FluentAssertions;
using PriceProof.Application.Cases;

namespace PriceProof.UnitTests.Cases;

public sealed class CreateCaseRequestValidatorTests
{
    [Fact]
    public void Should_reject_empty_identifiers_and_blank_description()
    {
        var validator = new CreateCaseRequestValidator();
        var request = new CreateCaseRequest(
            Guid.Empty,
            Guid.Empty,
            null,
            string.Empty,
            DateTimeOffset.UtcNow,
            "ZA",
            null,
            null);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateCaseRequest.ReportedByUserId));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateCaseRequest.MerchantId));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateCaseRequest.BasketDescription));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateCaseRequest.CurrencyCode));
    }

    [Fact]
    public void Should_accept_valid_case_payload()
    {
        var validator = new CreateCaseRequestValidator();
        var request = new CreateCaseRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "Basket of groceries charged above the displayed price",
            DateTimeOffset.UtcNow.AddMinutes(-5),
            "ZAR",
            "PP-12345",
            "Customer reported a card surcharge at the till.");

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_accept_valid_custom_merchant_payload()
    {
        var validator = new CreateCaseRequestValidator();
        var request = new CreateCaseRequest(
            Guid.NewGuid(),
            null,
            null,
            "Small grocery basket",
            DateTimeOffset.UtcNow.AddMinutes(-5),
            "ZAR",
            null,
            "Customer bought bread and milk from a local corner shop.",
            "Corner Supermarket");

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_reject_payload_when_selected_and_custom_merchant_are_both_supplied()
    {
        var validator = new CreateCaseRequestValidator();
        var request = new CreateCaseRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "Mixed merchant selection payload",
            DateTimeOffset.UtcNow.AddMinutes(-5),
            "ZAR",
            null,
            null,
            "Corner Supermarket");

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(CreateCaseRequest.CustomMerchantName));
    }
}
