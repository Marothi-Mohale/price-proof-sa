using Microsoft.EntityFrameworkCore;
using PriceProofSA.Application.Abstractions.Persistence;
using PriceProofSA.Domain.Entities;

namespace PriceProofSA.Infrastructure.Persistence;

public sealed class PriceProofDbContext : DbContext, IApplicationDbContext
{
    public PriceProofDbContext(DbContextOptions<PriceProofDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<Merchant> Merchants => Set<Merchant>();

    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<DiscrepancyCase> Cases => Set<DiscrepancyCase>();

    public DbSet<PriceCapture> PriceCaptures => Set<PriceCapture>();

    public DbSet<PriceEvidence> PriceEvidence => Set<PriceEvidence>();

    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();

    public DbSet<ReceiptRecord> ReceiptRecords => Set<ReceiptRecord>();

    public DbSet<ComplaintPack> ComplaintPacks => Set<ComplaintPack>();

    public DbSet<MerchantRiskScore> MerchantRiskScores => Set<MerchantRiskScore>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Email).HasMaxLength(320).IsRequired();
            builder.Property(item => item.NormalizedEmail).HasMaxLength(320).IsRequired();
            builder.Property(item => item.DisplayName).HasMaxLength(120).IsRequired();
            builder.HasIndex(item => item.NormalizedEmail).IsUnique();
            builder.Navigation(item => item.Sessions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<UserSession>(builder =>
        {
            builder.ToTable("user_sessions");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.TokenHash).HasMaxLength(128).IsRequired();
            builder.HasIndex(item => item.TokenHash).IsUnique();
            builder.HasOne(item => item.User)
                .WithMany(item => item.Sessions)
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Merchant>(builder =>
        {
            builder.ToTable("merchants");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Name).HasMaxLength(200).IsRequired();
            builder.Property(item => item.NormalizedName).HasMaxLength(200).IsRequired();
            builder.Property(item => item.Category).HasMaxLength(120);
            builder.HasIndex(item => item.NormalizedName).IsUnique();
            builder.Navigation(item => item.Branches).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<Branch>(builder =>
        {
            builder.ToTable("branches");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Name).HasMaxLength(200).IsRequired();
            builder.Property(item => item.AddressLine).HasMaxLength(240);
            builder.Property(item => item.City).HasMaxLength(120);
            builder.Property(item => item.Province).HasMaxLength(120);
            builder.HasOne(item => item.Merchant)
                .WithMany(item => item.Branches)
                .HasForeignKey(item => item.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DiscrepancyCase>(builder =>
        {
            builder.ToTable("cases");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.BasketDescription).HasMaxLength(400).IsRequired();
            builder.Property(item => item.QuotedAmount).HasPrecision(18, 2);
            builder.Property(item => item.ChargedAmount).HasPrecision(18, 2);
            builder.Property(item => item.DifferenceAmount).HasPrecision(18, 2);
            builder.Property(item => item.ComplaintSummary).HasMaxLength(2000);
            builder.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(item => item.Merchant)
                .WithMany()
                .HasForeignKey(item => item.MerchantId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(item => item.Branch)
                .WithMany()
                .HasForeignKey(item => item.BranchId)
                .OnDelete(DeleteBehavior.SetNull);
            builder.Navigation(item => item.PriceCaptures).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(item => item.PaymentRecords).UsePropertyAccessMode(PropertyAccessMode.Field);
            builder.Navigation(item => item.ComplaintPacks).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<PriceCapture>(builder =>
        {
            builder.ToTable("price_captures");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.CapturedAmount).HasPrecision(18, 2);
            builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsRequired();
            builder.Property(item => item.QuoteText).HasMaxLength(1000);
            builder.Property(item => item.Notes).HasMaxLength(1000);
            builder.Property(item => item.MerchantQrToken).HasMaxLength(200);
            builder.HasOne(item => item.Case)
                .WithMany(item => item.PriceCaptures)
                .HasForeignKey(item => item.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(item => item.Evidence).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<PriceEvidence>(builder =>
        {
            builder.ToTable("price_evidence");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.FileName).HasMaxLength(260).IsRequired();
            builder.Property(item => item.ContentType).HasMaxLength(120).IsRequired();
            builder.Property(item => item.StoragePath).HasMaxLength(500).IsRequired();
            builder.Property(item => item.ContentHash).HasMaxLength(128).IsRequired();
            builder.HasOne(item => item.PriceCapture)
                .WithMany(item => item.Evidence)
                .HasForeignKey(item => item.PriceCaptureId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PaymentRecord>(builder =>
        {
            builder.ToTable("payment_records");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Amount).HasPrecision(18, 2);
            builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsRequired();
            builder.Property(item => item.Note).HasMaxLength(1000);
            builder.Property(item => item.RedactedBankNotificationText).HasMaxLength(1000);
            builder.HasOne(item => item.Case)
                .WithMany(item => item.PaymentRecords)
                .HasForeignKey(item => item.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(item => item.ReceiptRecord)
                .WithOne(item => item.PaymentRecord)
                .HasForeignKey<ReceiptRecord>(item => item.PaymentRecordId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReceiptRecord>(builder =>
        {
            builder.ToTable("receipt_records");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.FileName).HasMaxLength(260).IsRequired();
            builder.Property(item => item.ContentType).HasMaxLength(120).IsRequired();
            builder.Property(item => item.StoragePath).HasMaxLength(500).IsRequired();
            builder.Property(item => item.ContentHash).HasMaxLength(128).IsRequired();
            builder.Property(item => item.ParsedTotalAmount).HasPrecision(18, 2);
            builder.Property(item => item.OcrRawText).HasMaxLength(8000);
        });

        modelBuilder.Entity<ComplaintPack>(builder =>
        {
            builder.ToTable("complaint_packs");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.FileName).HasMaxLength(260).IsRequired();
            builder.Property(item => item.StoragePath).HasMaxLength(500).IsRequired();
            builder.Property(item => item.Summary).HasMaxLength(2000).IsRequired();
            builder.Property(item => item.FileHash).HasMaxLength(128).IsRequired();
            builder.HasOne(item => item.Case)
                .WithMany(item => item.ComplaintPacks)
                .HasForeignKey(item => item.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MerchantRiskScore>(builder =>
        {
            builder.ToTable("merchant_risk_scores");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.Score).HasPrecision(18, 2);
            builder.Property(item => item.Trend).HasMaxLength(32).IsRequired();
            builder.HasIndex(item => item.MerchantId).IsUnique();
            builder.HasOne(item => item.Merchant)
                .WithOne(item => item.RiskScore)
                .HasForeignKey<MerchantRiskScore>(item => item.MerchantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.ToTable("audit_logs");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.EntityType).HasMaxLength(120).IsRequired();
            builder.Property(item => item.Action).HasMaxLength(120).IsRequired();
            builder.Property(item => item.PayloadJson).HasMaxLength(16000).IsRequired();
            builder.Property(item => item.Hash).HasMaxLength(128).IsRequired();
            builder.Property(item => item.PreviousHash).HasMaxLength(128);
            builder.HasIndex(item => item.OccurredAtUtc);
            builder.HasIndex(item => new { item.CaseId, item.OccurredAtUtc });
        });
    }
}
