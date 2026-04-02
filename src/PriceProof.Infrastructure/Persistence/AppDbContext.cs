using Microsoft.EntityFrameworkCore;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Domain.Entities;
using PriceProof.Infrastructure.Seeding;

namespace PriceProof.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Merchant> Merchants => Set<Merchant>();

    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<DiscrepancyCase> DiscrepancyCases => Set<DiscrepancyCase>();

    public DbSet<PriceCapture> PriceCaptures => Set<PriceCapture>();

    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();

    public DbSet<ReceiptRecord> ReceiptRecords => Set<ReceiptRecord>();

    public DbSet<ComplaintPack> ComplaintPacks => Set<ComplaintPack>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<MerchantRiskSnapshot> MerchantRiskSnapshots => Set<MerchantRiskSnapshot>();

    public DbSet<BranchRiskSnapshot> BranchRiskSnapshots => Set<BranchRiskSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
        ConfigureMerchants(modelBuilder);
        ConfigureBranches(modelBuilder);
        ConfigureCases(modelBuilder);
        ConfigurePriceCaptures(modelBuilder);
        ConfigurePaymentRecords(modelBuilder);
        ConfigureReceiptRecords(modelBuilder);
        ConfigureComplaintPacks(modelBuilder);
        ConfigureAuditLogs(modelBuilder);
        ConfigureMerchantRiskSnapshots(modelBuilder);
        ConfigureBranchRiskSnapshots(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.DisplayName).HasMaxLength(120).IsRequired();
            builder.Property(entity => entity.Email).HasMaxLength(320).IsRequired();
            builder.Property(entity => entity.NormalizedEmail).HasMaxLength(320).IsRequired();
            builder.Property(entity => entity.PasswordHash).HasMaxLength(256);
            builder.Property(entity => entity.PasswordSalt).HasMaxLength(128);
            builder.Property(entity => entity.PasswordIterations);
            builder.Property(entity => entity.LastSignedInUtc);
            builder.HasIndex(entity => entity.NormalizedEmail).IsUnique();
            builder.Property(entity => entity.IsAdmin).IsRequired();
            builder.Property(entity => entity.CreatedUtc).IsRequired();
            builder.Property(entity => entity.UpdatedUtc).IsRequired();
            builder.HasQueryFilter(entity => !entity.IsDeleted);

            builder.HasData(
                new
                {
                    Id = SeedData.AdminUserId,
                    DisplayName = "PriceProof Admin",
                    Email = "admin@priceproof.local",
                    NormalizedEmail = "ADMIN@PRICEPROOF.LOCAL",
                    PasswordHash = (string?)null,
                    PasswordSalt = (string?)null,
                    PasswordIterations = (int?)null,
                    LastSignedInUtc = (DateTimeOffset?)null,
                    IsActive = true,
                    IsAdmin = true,
                    CreatedUtc = SeedData.SeedTimestamp,
                    UpdatedUtc = SeedData.SeedTimestamp,
                    IsDeleted = false,
                    DeletedUtc = (DateTimeOffset?)null
                },
                new
                {
                    Id = SeedData.DemoUserId,
                    DisplayName = "Demo Investigator",
                    Email = "investigator@priceproof.local",
                    NormalizedEmail = "INVESTIGATOR@PRICEPROOF.LOCAL",
                    PasswordHash = (string?)null,
                    PasswordSalt = (string?)null,
                    PasswordIterations = (int?)null,
                    LastSignedInUtc = (DateTimeOffset?)null,
                    IsActive = true,
                    IsAdmin = false,
                    CreatedUtc = SeedData.SeedTimestamp,
                    UpdatedUtc = SeedData.SeedTimestamp,
                    IsDeleted = false,
                    DeletedUtc = (DateTimeOffset?)null
                });
        });
    }

    private static void ConfigureMerchants(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Merchant>(builder =>
        {
            builder.ToTable("merchants");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
            builder.Property(entity => entity.NormalizedName).HasMaxLength(200).IsRequired();
            builder.Property(entity => entity.Category).HasMaxLength(80);
            builder.Property(entity => entity.WebsiteUrl).HasMaxLength(256);
            builder.Property(entity => entity.CurrentRiskScore).HasPrecision(5, 2);
            builder.Property(entity => entity.CurrentRiskLabel);
            builder.Property(entity => entity.RiskUpdatedUtc);
            builder.HasIndex(entity => entity.NormalizedName).IsUnique();
            builder.Property(entity => entity.CreatedUtc).IsRequired();
            builder.Property(entity => entity.UpdatedUtc).IsRequired();
            builder.HasQueryFilter(entity => !entity.IsDeleted);

            builder.HasData(
                new
                {
                    Id = SeedData.ShopriteMerchantId,
                    Name = "Shoprite",
                    NormalizedName = "SHOPRITE",
                    Category = "Groceries",
                    WebsiteUrl = "https://www.shoprite.co.za",
                    CurrentRiskScore = (decimal?)null,
                    CurrentRiskLabel = (int?)null,
                    RiskUpdatedUtc = (DateTimeOffset?)null,
                    CreatedUtc = SeedData.SeedTimestamp,
                    UpdatedUtc = SeedData.SeedTimestamp,
                    IsDeleted = false,
                    DeletedUtc = (DateTimeOffset?)null
                },
                new
                {
                    Id = SeedData.DisChemMerchantId,
                    Name = "Dis-Chem",
                    NormalizedName = "DIS-CHEM",
                    Category = "Pharmacy",
                    WebsiteUrl = "https://www.dischem.co.za",
                    CurrentRiskScore = (decimal?)null,
                    CurrentRiskLabel = (int?)null,
                    RiskUpdatedUtc = (DateTimeOffset?)null,
                    CreatedUtc = SeedData.SeedTimestamp,
                    UpdatedUtc = SeedData.SeedTimestamp,
                    IsDeleted = false,
                    DeletedUtc = (DateTimeOffset?)null
                },
                new
                {
                    Id = SeedData.CheckersMerchantId,
                    Name = "Checkers",
                    NormalizedName = "CHECKERS",
                    Category = "Retail",
                    WebsiteUrl = "https://www.checkers.co.za",
                    CurrentRiskScore = (decimal?)null,
                    CurrentRiskLabel = (int?)null,
                    RiskUpdatedUtc = (DateTimeOffset?)null,
                    CreatedUtc = SeedData.SeedTimestamp,
                    UpdatedUtc = SeedData.SeedTimestamp,
                    IsDeleted = false,
                    DeletedUtc = (DateTimeOffset?)null
                });
        });
    }

    private static void ConfigureBranches(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Branch>(builder =>
        {
            builder.ToTable("branches");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
            builder.Property(entity => entity.Code).HasMaxLength(32);
            builder.Property(entity => entity.AddressLine1).HasMaxLength(200).IsRequired();
            builder.Property(entity => entity.AddressLine2).HasMaxLength(200);
            builder.Property(entity => entity.City).HasMaxLength(120).IsRequired();
            builder.Property(entity => entity.Province).HasMaxLength(120).IsRequired();
            builder.Property(entity => entity.PostalCode).HasMaxLength(20);
            builder.Property(entity => entity.CurrentRiskScore).HasPrecision(5, 2);
            builder.Property(entity => entity.CurrentRiskLabel);
            builder.Property(entity => entity.RiskUpdatedUtc);
            builder.Property(entity => entity.CreatedUtc).IsRequired();
            builder.Property(entity => entity.UpdatedUtc).IsRequired();
            builder.HasQueryFilter(entity => !entity.IsDeleted);
            builder.HasOne(entity => entity.Merchant)
                .WithMany(entity => entity.Branches)
                .HasForeignKey(entity => entity.MerchantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasData(
                new
                {
                    Id = SeedData.ShopriteSandtonBranchId,
                    MerchantId = SeedData.ShopriteMerchantId,
                    Name = "Shoprite Sandton City",
                    Code = "JHB-SANDTON",
                    AddressLine1 = "Sandton City, Rivonia Road",
                    AddressLine2 = (string?)null,
                    City = "Johannesburg",
                    Province = "Gauteng",
                    PostalCode = "2196",
                    CurrentRiskScore = (decimal?)null,
                    CurrentRiskLabel = (int?)null,
                    RiskUpdatedUtc = (DateTimeOffset?)null,
                    CreatedUtc = SeedData.SeedTimestamp,
                    UpdatedUtc = SeedData.SeedTimestamp,
                    IsDeleted = false,
                    DeletedUtc = (DateTimeOffset?)null
                },
                new
                {
                    Id = SeedData.ShopritePretoriaBranchId,
                    MerchantId = SeedData.ShopriteMerchantId,
                    Name = "Shoprite Pretoria CBD",
                    Code = "PTA-CBD",
                    AddressLine1 = "251 Paul Kruger Street",
                    AddressLine2 = (string?)null,
                    City = "Pretoria",
                    Province = "Gauteng",
                    PostalCode = "0002",
                    CurrentRiskScore = (decimal?)null,
                    CurrentRiskLabel = (int?)null,
                    RiskUpdatedUtc = (DateTimeOffset?)null,
                    CreatedUtc = SeedData.SeedTimestamp,
                    UpdatedUtc = SeedData.SeedTimestamp,
                    IsDeleted = false,
                    DeletedUtc = (DateTimeOffset?)null
                },
                new
                {
                    Id = SeedData.DisChemRosebankBranchId,
                    MerchantId = SeedData.DisChemMerchantId,
                    Name = "Dis-Chem Rosebank Mall",
                    Code = "JHB-ROSEBANK",
                    AddressLine1 = "50 Bath Avenue",
                    AddressLine2 = (string?)null,
                    City = "Johannesburg",
                    Province = "Gauteng",
                    PostalCode = "2196",
                    CurrentRiskScore = (decimal?)null,
                    CurrentRiskLabel = (int?)null,
                    RiskUpdatedUtc = (DateTimeOffset?)null,
                    CreatedUtc = SeedData.SeedTimestamp,
                    UpdatedUtc = SeedData.SeedTimestamp,
                    IsDeleted = false,
                    DeletedUtc = (DateTimeOffset?)null
                },
                new
                {
                    Id = SeedData.CheckersSeaPointBranchId,
                    MerchantId = SeedData.CheckersMerchantId,
                    Name = "Checkers Sea Point",
                    Code = "CPT-SEA-POINT",
                    AddressLine1 = "154 Main Road",
                    AddressLine2 = (string?)null,
                    City = "Cape Town",
                    Province = "Western Cape",
                    PostalCode = "8005",
                    CurrentRiskScore = (decimal?)null,
                    CurrentRiskLabel = (int?)null,
                    RiskUpdatedUtc = (DateTimeOffset?)null,
                    CreatedUtc = SeedData.SeedTimestamp,
                    UpdatedUtc = SeedData.SeedTimestamp,
                    IsDeleted = false,
                    DeletedUtc = (DateTimeOffset?)null
                });
        });
    }

    private static void ConfigureCases(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiscrepancyCase>(builder =>
        {
            builder.ToTable("cases");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.CaseNumber).HasMaxLength(20).IsRequired();
            builder.HasIndex(entity => entity.CaseNumber).IsUnique();
            builder.Property(entity => entity.BasketDescription).HasMaxLength(500).IsRequired();
            builder.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsRequired();
            builder.Property(entity => entity.CustomerReference).HasMaxLength(64);
            builder.Property(entity => entity.Notes).HasMaxLength(2000);
            builder.Property(entity => entity.LatestQuotedAmount).HasPrecision(18, 2);
            builder.Property(entity => entity.LatestPaidAmount).HasPrecision(18, 2);
            builder.Property(entity => entity.DifferenceAmount).HasPrecision(18, 2);
            builder.Property(entity => entity.AnalysisConfidence).HasPrecision(5, 4);
            builder.Property(entity => entity.AnalysisExplanation).HasMaxLength(2000);
            builder.Property(entity => entity.CreatedUtc).IsRequired();
            builder.Property(entity => entity.UpdatedUtc).IsRequired();
            builder.HasQueryFilter(entity => !entity.IsDeleted);
            builder.HasOne(entity => entity.ReportedByUser)
                .WithMany(entity => entity.ReportedCases)
                .HasForeignKey(entity => entity.ReportedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(entity => entity.Merchant)
                .WithMany(entity => entity.Cases)
                .HasForeignKey(entity => entity.MerchantId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(entity => entity.Branch)
                .WithMany(entity => entity.Cases)
                .HasForeignKey(entity => entity.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePriceCaptures(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PriceCapture>(builder =>
        {
            builder.ToTable("price_captures");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsRequired();
            builder.Property(entity => entity.FileName).HasMaxLength(260).IsRequired();
            builder.Property(entity => entity.ContentType).HasMaxLength(120);
            builder.Property(entity => entity.EvidenceStoragePath).HasMaxLength(500).IsRequired();
            builder.Property(entity => entity.EvidenceHash).HasMaxLength(128);
            builder.Property(entity => entity.MerchantStatement).HasMaxLength(2000);
            builder.Property(entity => entity.Notes).HasMaxLength(2000);
            builder.Property(entity => entity.QuotedAmount).HasPrecision(18, 2);
            builder.Property(entity => entity.CreatedUtc).IsRequired();
            builder.Property(entity => entity.UpdatedUtc).IsRequired();
            builder.HasQueryFilter(entity => !entity.Case!.IsDeleted && !entity.CapturedByUser!.IsDeleted);
            builder.HasOne(entity => entity.Case)
                .WithMany(entity => entity.PriceCaptures)
                .HasForeignKey(entity => entity.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(entity => entity.CapturedByUser)
                .WithMany(entity => entity.PriceCaptures)
                .HasForeignKey(entity => entity.CapturedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePaymentRecords(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentRecord>(builder =>
        {
            builder.ToTable("payment_records");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Amount).HasPrecision(18, 2);
            builder.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsRequired();
            builder.Property(entity => entity.PaymentReference).HasMaxLength(64);
            builder.Property(entity => entity.MerchantReference).HasMaxLength(64);
            builder.Property(entity => entity.CardLastFour).HasMaxLength(4);
            builder.Property(entity => entity.Notes).HasMaxLength(2000);
            builder.Property(entity => entity.CreatedUtc).IsRequired();
            builder.Property(entity => entity.UpdatedUtc).IsRequired();
            builder.HasQueryFilter(entity => !entity.Case!.IsDeleted && !entity.RecordedByUser!.IsDeleted);
            builder.HasOne(entity => entity.Case)
                .WithMany(entity => entity.PaymentRecords)
                .HasForeignKey(entity => entity.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(entity => entity.RecordedByUser)
                .WithMany(entity => entity.PaymentRecords)
                .HasForeignKey(entity => entity.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureReceiptRecords(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReceiptRecord>(builder =>
        {
            builder.ToTable("receipt_records");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.FileName).HasMaxLength(260).IsRequired();
            builder.Property(entity => entity.ContentType).HasMaxLength(120).IsRequired();
            builder.Property(entity => entity.StoragePath).HasMaxLength(500).IsRequired();
            builder.Property(entity => entity.FileHash).HasMaxLength(128);
            builder.Property(entity => entity.CurrencyCode).HasMaxLength(3).IsRequired();
            builder.Property(entity => entity.ParsedTotalAmount).HasPrecision(18, 2);
            builder.Property(entity => entity.ReceiptNumber).HasMaxLength(64);
            builder.Property(entity => entity.MerchantName).HasMaxLength(200);
            builder.Property(entity => entity.RawText).HasMaxLength(16000);
            builder.Property(entity => entity.TransactionAtUtc);
            builder.Property(entity => entity.OcrProviderName).HasMaxLength(80);
            builder.Property(entity => entity.OcrConfidence).HasPrecision(5, 4);
            builder.Property(entity => entity.OcrPayloadMetadataJson).HasMaxLength(32000);
            builder.Property(entity => entity.OcrLineItemsJson).HasMaxLength(16000);
            builder.Property(entity => entity.OcrProcessedUtc);
            builder.Property(entity => entity.CreatedUtc).IsRequired();
            builder.Property(entity => entity.UpdatedUtc).IsRequired();
            builder.HasQueryFilter(entity => !entity.Case!.IsDeleted && !entity.UploadedByUser!.IsDeleted);
            builder.HasOne(entity => entity.Case)
                .WithMany()
                .HasForeignKey(entity => entity.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(entity => entity.PaymentRecord)
                .WithOne(entity => entity.ReceiptRecord)
                .HasForeignKey<ReceiptRecord>(entity => entity.PaymentRecordId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(entity => entity.UploadedByUser)
                .WithMany(entity => entity.ReceiptRecords)
                .HasForeignKey(entity => entity.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureComplaintPacks(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ComplaintPack>(builder =>
        {
            builder.ToTable("complaint_packs");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.FileName).HasMaxLength(260).IsRequired();
            builder.Property(entity => entity.StoragePath).HasMaxLength(500).IsRequired();
            builder.Property(entity => entity.ContentHash).HasMaxLength(128).IsRequired();
            builder.Property(entity => entity.Summary).HasMaxLength(2000).IsRequired();
            builder.Property(entity => entity.CreatedUtc).IsRequired();
            builder.Property(entity => entity.UpdatedUtc).IsRequired();
            builder.HasQueryFilter(entity => !entity.IsDeleted);
            builder.HasOne(entity => entity.Case)
                .WithMany(entity => entity.ComplaintPacks)
                .HasForeignKey(entity => entity.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(entity => entity.GeneratedByUser)
                .WithMany()
                .HasForeignKey(entity => entity.GeneratedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureAuditLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.ToTable("audit_logs");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.EntityName).HasMaxLength(120).IsRequired();
            builder.Property(entity => entity.Action).HasMaxLength(80).IsRequired();
            builder.Property(entity => entity.PayloadJson).HasMaxLength(32000).IsRequired();
            builder.Property(entity => entity.CorrelationId).HasMaxLength(64).IsRequired();
            builder.Property(entity => entity.CreatedUtc).IsRequired();
            builder.Property(entity => entity.UpdatedUtc).IsRequired();
            builder.HasIndex(entity => entity.OccurredAtUtc);
            builder.HasOne(entity => entity.Case)
                .WithMany(entity => entity.AuditLogs)
                .HasForeignKey(entity => entity.CaseId)
                .OnDelete(DeleteBehavior.SetNull);
            builder.HasOne(entity => entity.ActorUser)
                .WithMany(entity => entity.AuditLogs)
                .HasForeignKey(entity => entity.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureMerchantRiskSnapshots(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MerchantRiskSnapshot>(builder =>
        {
            builder.ToTable("merchant_risk_snapshots");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.ModelVersion).HasMaxLength(32).IsRequired();
            builder.Property(entity => entity.ConfidenceWeightedMismatchTotal).HasPrecision(18, 2);
            builder.Property(entity => entity.RecencyWeightedCaseCount).HasPrecision(8, 4);
            builder.Property(entity => entity.DismissedEquivalentRatio).HasPrecision(5, 4);
            builder.Property(entity => entity.UnclearCaseRatio).HasPrecision(5, 4);
            builder.Property(entity => entity.Score).HasPrecision(5, 2);
            builder.Property(entity => entity.CreatedUtc).IsRequired();
            builder.Property(entity => entity.UpdatedUtc).IsRequired();
            builder.HasIndex(entity => new { entity.MerchantId, entity.CalculatedUtc });
            builder.HasOne(entity => entity.Merchant)
                .WithMany(entity => entity.RiskSnapshots)
                .HasForeignKey(entity => entity.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureBranchRiskSnapshots(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BranchRiskSnapshot>(builder =>
        {
            builder.ToTable("branch_risk_snapshots");
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.ModelVersion).HasMaxLength(32).IsRequired();
            builder.Property(entity => entity.ConfidenceWeightedMismatchTotal).HasPrecision(18, 2);
            builder.Property(entity => entity.RecencyWeightedCaseCount).HasPrecision(8, 4);
            builder.Property(entity => entity.DismissedEquivalentRatio).HasPrecision(5, 4);
            builder.Property(entity => entity.UnclearCaseRatio).HasPrecision(5, 4);
            builder.Property(entity => entity.Score).HasPrecision(5, 2);
            builder.Property(entity => entity.CreatedUtc).IsRequired();
            builder.Property(entity => entity.UpdatedUtc).IsRequired();
            builder.HasIndex(entity => new { entity.BranchId, entity.CalculatedUtc });
            builder.HasOne(entity => entity.Branch)
                .WithMany(entity => entity.RiskSnapshots)
                .HasForeignKey(entity => entity.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
