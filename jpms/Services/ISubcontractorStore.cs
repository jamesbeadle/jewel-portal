using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface ISubcontractorStore
{
    /// <summary>False until the directory has been fetched at least once. Lets views
    /// distinguish "still loading" from "genuinely not found".</summary>
    bool IsLoaded { get; }

    IReadOnlyList<Subcontractor> All();
    Subcontractor? Find(string subcontractorId);
    Subcontractor Upsert(Subcontractor subcontractor);
    IReadOnlyList<ComplianceDocument> ComplianceFor(string subcontractorId);
    void SaveCompliance(ComplianceDocument document);
    event Action? OnChange;
}
