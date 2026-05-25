using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface ISubcontractorStore
{
    IReadOnlyList<Subcontractor> All();
    Subcontractor? Find(string subcontractorId);
    Subcontractor Upsert(Subcontractor subcontractor);
    IReadOnlyList<ComplianceDocument> ComplianceFor(string subcontractorId);
    void SaveCompliance(ComplianceDocument document);
    event Action? OnChange;
}
