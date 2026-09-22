using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Features.Hs.Audits;

/// <summary>Every project's audits, newest first — the officer's home reads the last one per
/// site from it. Null before a fetch lands; gate on Current.</summary>
public sealed class HsAuditsAcrossProjectsReadModel : IReadModelStore<IReadOnlyList<HsAudit>>
{
    private readonly IQueryClient queries;
    public IReadOnlyList<HsAudit>? Current { get; private set; }
    public event Action? OnChanged;

    public HsAuditsAcrossProjectsReadModel(IQueryClient queries) { this.queries = queries; }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListHsAuditsAcrossProjects(), cancellationToken);
        OnChanged?.Invoke();
    }
}
