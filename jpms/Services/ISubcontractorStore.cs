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

    /// <summary>The curated master list of trades (sorted by name).</summary>
    IReadOnlyList<Trade> Trades();

    /// <summary>Adds a trade to the curated list; returns the existing trade if the name already exists.</summary>
    Task<Trade> AddTradeAsync(string name);

    /// <summary>Replaces a directory record's trades with exactly the given set.</summary>
    Task SetTradesAsync(string subcontractorId, IReadOnlyList<string> tradeIds);
    IReadOnlyList<ComplianceDocument> ComplianceFor(string subcontractorId);
    void SaveCompliance(ComplianceDocument document);
    event Action? OnChange;
}
