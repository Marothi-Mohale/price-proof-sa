using System.Globalization;
using System.Text;
using PriceProofSA.Application.Abstractions.Complaints;

namespace PriceProofSA.Infrastructure.Complaints;

public sealed class SimplePdfComplaintPackGenerator : IComplaintPackGenerator
{
    public Task<GeneratedDocument> GenerateAsync(ComplaintPackBuildRequest request, CancellationToken cancellationToken = default)
    {
        var lines = new List<string>
        {
            "PriceProof SA Evidence Pack",
            $"Case ID: {request.CaseId}",
            $"Store: {request.StoreName}",
            $"Basket: {request.BasketDescription}",
            $"Quoted / Displayed Price: R{request.QuotedAmount.ToString("0.00", CultureInfo.InvariantCulture)}",
            $"Charged Amount: R{request.ChargedAmount.ToString("0.00", CultureInfo.InvariantCulture)}",
            $"Difference: R{request.DifferenceAmount.ToString("0.00", CultureInfo.InvariantCulture)}",
            $"Classification: {request.Classification}",
            $"Created: {request.CreatedAtUtc:O}",
            $"Updated: {request.LastUpdatedAtUtc:O}",
            string.Empty,
            "Complaint Summary",
            request.Summary,
            string.Empty,
            "Attachments"
        };

        foreach (var attachment in request.Attachments)
        {
            lines.Add($"- {attachment.Label}: {attachment.FileName} ({attachment.ContentType}) at {attachment.CapturedAtUtc:O}");
        }

        var bytes = BuildSinglePagePdf(lines);
        var safeMerchantName = string.Join("-", request.StoreName.Split(Path.GetInvalidFileNameChars().Concat([' ']).ToArray(), StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();

        return Task.FromResult(new GeneratedDocument(
            $"{safeMerchantName}-evidence-pack.pdf",
            "application/pdf",
            bytes,
            request.Summary));
    }

    private static byte[] BuildSinglePagePdf(IReadOnlyCollection<string> lines)
    {
        var contentBuilder = new StringBuilder();
        contentBuilder.AppendLine("BT");
        contentBuilder.AppendLine("/F1 11 Tf");
        contentBuilder.AppendLine("50 760 Td");

        var isFirstLine = true;
        foreach (var line in lines)
        {
            if (!isFirstLine)
            {
                contentBuilder.AppendLine("0 -16 Td");
            }

            contentBuilder.Append('(')
                .Append(EscapePdfText(string.IsNullOrWhiteSpace(line) ? " " : line))
                .AppendLine(") Tj");

            isFirstLine = false;
        }

        contentBuilder.AppendLine("ET");
        var content = contentBuilder.ToString();

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);

        writer.WriteLine("%PDF-1.4");
        var offsets = new List<long> { 0 };
        WriteObject(writer, offsets, 1, "<< /Type /Catalog /Pages 2 0 R >>");
        WriteObject(writer, offsets, 2, "<< /Type /Pages /Count 1 /Kids [3 0 R] >>");
        WriteObject(writer, offsets, 3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>");
        WriteObject(writer, offsets, 4, $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream");
        WriteObject(writer, offsets, 5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        var xrefStart = stream.Position;
        writer.WriteLine("xref");
        writer.WriteLine($"0 {offsets.Count}");
        writer.WriteLine("0000000000 65535 f ");

        for (var i = 1; i < offsets.Count; i++)
        {
            writer.WriteLine($"{offsets[i]:0000000000} 00000 n ");
        }

        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {offsets.Count} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefStart);
        writer.Write("%%EOF");
        writer.Flush();

        return stream.ToArray();
    }

    private static void WriteObject(StreamWriter writer, List<long> offsets, int index, string body)
    {
        writer.Flush();
        offsets.Add(writer.BaseStream.Position);
        writer.WriteLine($"{index} 0 obj");
        writer.WriteLine(body);
        writer.WriteLine("endobj");
    }

    private static string EscapePdfText(string input)
    {
        return input.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
