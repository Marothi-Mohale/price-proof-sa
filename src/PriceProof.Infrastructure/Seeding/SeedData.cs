namespace PriceProof.Infrastructure.Seeding;

public static class SeedData
{
    public static readonly DateTimeOffset SeedTimestamp = new(2025, 1, 15, 8, 0, 0, TimeSpan.Zero);

    public static readonly Guid AdminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DemoUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static readonly Guid ShopriteMerchantId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid DisChemMerchantId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid CheckersMerchantId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    public static readonly Guid ShopriteSandtonBranchId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid ShopritePretoriaBranchId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    public static readonly Guid DisChemRosebankBranchId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    public static readonly Guid CheckersSeaPointBranchId = Guid.Parse("99999999-9999-9999-9999-999999999999");
}
