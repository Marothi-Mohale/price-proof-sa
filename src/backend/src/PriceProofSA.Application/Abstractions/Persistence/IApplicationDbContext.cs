using Microsoft.EntityFrameworkCore;
using PriceProofSA.Domain.Entities;

namespace PriceProofSA.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<AppUser> Users { get; }

    DbSet<UserSession> UserSessions { get; }

    DbSet<Merchant> Merchants { get; }

    DbSet<Branch> Branches { get; }

    DbSet<DiscrepancyCase> Cases { get; }

    DbSet<PriceCapture> PriceCaptures { get; }

    DbSet<PriceEvidence> PriceEvidence { get; }

    DbSet<PaymentRecord> PaymentRecords { get; }

    DbSet<ReceiptRecord> ReceiptRecords { get; }

    DbSet<ComplaintPack> ComplaintPacks { get; }

    DbSet<MerchantRiskScore> MerchantRiskScores { get; }

    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
