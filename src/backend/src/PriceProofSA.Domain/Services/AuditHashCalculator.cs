using System.Security.Cryptography;
using System.Text;

namespace PriceProofSA.Domain.Services;

public static class AuditHashCalculator
{
    public static string Compute(
        string? previousHash,
        Guid? userId,
        Guid? caseId,
        string entityType,
        Guid entityId,
        string action,
        string payloadJson,
        DateTimeOffset occurredAtUtc)
    {
        var source =
            $"{previousHash}|{userId}|{caseId}|{entityType}|{entityId}|{action}|{payloadJson}|{occurredAtUtc:O}";

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(bytes);
    }
}
