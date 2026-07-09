using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public interface IVariationStore
{
    event Action? OnChange;

    Task<VariationOrderQuote?> GetByIdAsync(string voqId, CancellationToken cancellationToken = default);
    Task<VariationOrderQuote?> GetByRequestAsync(string requestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VariationOrderQuote>> ListForProjectAsync(string projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VariationOrder>> ListVariationOrdersForProjectAsync(string projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BidPackage>> ListBidPackagesAsync(string voqId, CancellationToken cancellationToken = default);

    /// <summary>AI-drafts a VOQ from the request and its tagged emails; nothing is saved.</summary>
    Task<VoqDraftProposal> PrepareVoqDraftAsync(string requestId, CancellationToken cancellationToken = default);
    Task<VariationOrderQuote> CreateFromRfqAsync(string requestId, string? title = null, string? description = null, decimal? estimatedValue = null, CancellationToken cancellationToken = default);
    Task<BidPackage> AddBidPackageAsync(string voqId, string title, string trade, CancellationToken cancellationToken = default);
    Task<VariationOrderQuote> SelectTenderAsync(string voqId, string bidPackageId, string subcontractorId, decimal? estimatedValue, CancellationToken cancellationToken = default);

    Task<VariationOrder?> GetVariationOrderByVoqAsync(string voqId, CancellationToken cancellationToken = default);
    Task<VariationOrder> ApproveVoqAsync(string voqId, string costCode, decimal? value, CancellationToken cancellationToken = default);
    Task<VariationOrder> IssueVariationOrderAsync(string voId, CancellationToken cancellationToken = default);
    Task<VariationOrder> CancelVariationOrderAsync(string voId, CancellationToken cancellationToken = default);
}
