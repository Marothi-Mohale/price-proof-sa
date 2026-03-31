using Microsoft.EntityFrameworkCore;
using PriceProof.Domain.Entities;

namespace PriceProof.Application.Abstractions.Persistence;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    DbSet<Merchant> Merchants { get; }

    DbSet<Branch> Branches { get; }

    DbSet<DiscrepancyCase> DiscrepancyCases { get; }

    DbSet<PriceCapture> PriceCaptures { get; }

    DbSet<PaymentRecord> PaymentRecords { get; }

    DbSet<ReceiptRecord> ReceiptRecords { get; }

    DbSet<ComplaintPack> ComplaintPacks { get; }

    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
