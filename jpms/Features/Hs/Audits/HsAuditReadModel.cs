using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Features.Hs.Audits;

/// <summary>The project's audit list and each audit's form, cached by key — the H&S tab reads
/// the list, the audit page reads one view. Null before a fetch lands; gate on LoadedFor.</summary>
public sealed class HsAuditReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, IReadOnlyList<HsAudit>> auditsByProject = new();
    private readonly Dictionary<string, HsAuditView> viewByAudit = new();

    public HsAuditReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public IReadOnlyList<HsAudit>? AuditsFor(string projectId) =>
        auditsByProject.TryGetValue(projectId, out var audits) ? audits : null;

    public bool LoadedFor(string projectId) => auditsByProject.ContainsKey(projectId);

    public HsAuditView? View(string hsAuditId) =>
        viewByAudit.TryGetValue(hsAuditId, out var view) ? view : null;

    public bool ViewLoadedFor(string hsAuditId) => viewByAudit.ContainsKey(hsAuditId);

    public async Task RefreshAsync(string projectId, CancellationToken cancellationToken)
    {
        auditsByProject[projectId] = await queries.AskAsync(new ListHsAuditsForProject(projectId), cancellationToken);
        OnChanged?.Invoke();
    }

    public async Task RefreshViewAsync(string hsAuditId, CancellationToken cancellationToken)
    {
        viewByAudit[hsAuditId] = await queries.AskAsync(new GetHsAudit(hsAuditId), cancellationToken);
        OnChanged?.Invoke();
    }

    /// <summary>A write's own answer, kept so the page never re-fetches what it was just told.</summary>
    public void Accept(HsAuditView view)
    {
        viewByAudit[view.Audit.HsAuditId] = view;
        OnChanged?.Invoke();
    }
}
