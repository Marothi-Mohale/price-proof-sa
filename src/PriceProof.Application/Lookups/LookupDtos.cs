namespace PriceProof.Application.Lookups;

public sealed record LookupUserDto(
    Guid Id,
    string DisplayName,
    string Email,
    bool IsActive);

public sealed record LookupBranchDto(
    Guid Id,
    Guid MerchantId,
    string Name,
    string? Code,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string Province,
    string? PostalCode);

public sealed record LookupMerchantDto(
    Guid Id,
    string Name,
    string? Category,
    string? WebsiteUrl,
    IReadOnlyCollection<LookupBranchDto> Branches);

public sealed record BootstrapLookupsDto(
    IReadOnlyCollection<LookupUserDto> Users,
    IReadOnlyCollection<LookupMerchantDto> Merchants);
