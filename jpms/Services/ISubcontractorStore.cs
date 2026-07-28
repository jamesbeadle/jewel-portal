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

    /// <summary>False until the trade list has been fetched at least once. Trades() answers with an
    /// empty list in the meantime, so a picker built from it needs this to tell "no trades yet"
    /// from "not asked yet" rather than offering nothing and looking like the whole list.</summary>
    bool TradesLoaded { get; }

    /// <summary>Adds a trade to the curated list; returns the existing trade if the name already exists.</summary>
    Task<Trade> AddTradeAsync(string name);

    /// <summary>Replaces a directory record's trades with exactly the given set.</summary>
    Task SetTradesAsync(string subcontractorId, IReadOnlyList<string> tradeIds);

    /// <summary>Updates a directory record's company name, contact details and payment terms
    /// (the days printed on its purchase orders), preserving its trades and every other field.
    /// Server-side this goes through UpdateSubcontractor, which is restricted to Admin, MD,
    /// FD and PM.</summary>
    Task UpdateDetailsAsync(string subcontractorId, string companyName,
        string contactName, string contactEmail, string contactPhone, int paymentTermsDays);
    IReadOnlyList<ComplianceDocument> ComplianceFor(string subcontractorId);
    void SaveCompliance(ComplianceDocument document);
    event Action? OnChange;
}
