namespace PriceProof.Infrastructure.Options;

public sealed class ComplaintPackOptions
{
    public const string SectionName = "ComplaintPacks";

    public bool Enabled { get; set; } = true;

    public string StorageRootPath { get; set; } = "storage";

    public string Title { get; set; } = "PriceProof SA Complaint Pack";

    public string LogoText { get; set; } = "PP";

    public bool IncludeEvidencePreviews { get; set; } = true;

    public bool IncludeEvidenceReferences { get; set; } = true;
}
