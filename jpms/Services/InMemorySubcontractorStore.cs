using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class InMemorySubcontractorStore : ISubcontractorStore
{
    private readonly List<Subcontractor> subcontractors = new()
    {
        new("SC-001", "Surrey Concrete Ltd",    "Groundworks",   "Pete Maynard",   "pete@surreyconcrete.example", "+44 1483 555 010", "Verified", DateTimeOffset.UtcNow.AddYears(-2)),
        new("SC-002", "Hampton Brick Supply",   "Masonry",       "Linda Hampton",  "linda@hamptonbrick.example",  "+44 1483 555 020", "Verified", DateTimeOffset.UtcNow.AddYears(-1)),
        new("SC-003", "Cobham Electrical",      "Electrical",    "Raj Patel",      "raj@cobhamelectrical.example","+44 1483 555 030", "Verified", DateTimeOffset.UtcNow.AddMonths(-6)),
        new("SC-004", "Hartfield Joinery",      "Joinery",       "Tom Hartfield",  "tom@hartfield.example",       "+44 1483 555 040", "Pending",  DateTimeOffset.UtcNow.AddMonths(-2))
    };

    private readonly List<ComplianceDocument> documents = new()
    {
        new("CD-001", "SC-001", "Public Liability Insurance", "pli-2026.pdf", DateTimeOffset.UtcNow.AddMonths(11),  DateTimeOffset.UtcNow.AddDays(-30)),
        new("CD-002", "SC-001", "Employers Liability",        "el-2026.pdf",  DateTimeOffset.UtcNow.AddMonths(11),  DateTimeOffset.UtcNow.AddDays(-30)),
        new("CD-003", "SC-002", "Public Liability Insurance", "pli-2026.pdf", DateTimeOffset.UtcNow.AddDays(20),    DateTimeOffset.UtcNow.AddDays(-340)),
        new("CD-004", "SC-003", "Public Liability Insurance", "pli-2026.pdf", DateTimeOffset.UtcNow.AddMonths(6),   DateTimeOffset.UtcNow.AddDays(-120)),
        new("CD-005", "SC-004", "Public Liability Insurance", "pli-2024.pdf", DateTimeOffset.UtcNow.AddDays(-15),   DateTimeOffset.UtcNow.AddDays(-380))
    };

    public event Action? OnChange;

    public IReadOnlyList<Subcontractor> All() =>
        subcontractors.OrderBy(sub => sub.CompanyName).ToList().AsReadOnly();

    public Subcontractor? Find(string subcontractorId) =>
        subcontractors.FirstOrDefault(sub =>
            string.Equals(sub.SubcontractorId, subcontractorId, StringComparison.OrdinalIgnoreCase));

    public Subcontractor Upsert(Subcontractor subcontractor)
    {
        var existing = Find(subcontractor.SubcontractorId);
        if (existing is not null) subcontractors.Remove(existing);
        subcontractors.Add(subcontractor);
        OnChange?.Invoke();
        return subcontractor;
    }

    public IReadOnlyList<ComplianceDocument> ComplianceFor(string subcontractorId) =>
        documents.Where(document =>
            string.Equals(document.SubcontractorId, subcontractorId, StringComparison.OrdinalIgnoreCase))
                 .ToList()
                 .AsReadOnly();

    public void SaveCompliance(ComplianceDocument document)
    {
        var existing = documents.FirstOrDefault(d => d.ComplianceDocumentId == document.ComplianceDocumentId);
        if (existing is not null) documents.Remove(existing);
        documents.Add(document);
        OnChange?.Invoke();
    }
}
