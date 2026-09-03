using Jewel.JPMS.Contracts.Kpi;

namespace Jewel.JPMS.Features.Kpi;

/// <summary>
/// The KPI register (administrators only, 2026-09-03): every email marked as a KPI against a
/// person at Jewel. One company-wide list — the admin page filters by person client-side.
/// Nullable Current = no fetch has landed yet (gate on it).
/// </summary>
public sealed class KpiReadModel : IReadModelStore<IReadOnlyList<KpiEmail>>
{
    private readonly IQueryClient queries;

    public KpiReadModel(IQueryClient queries) { this.queries = queries; }

    public IReadOnlyList<KpiEmail>? Current { get; private set; }

    public event Action? OnChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListKpiEmails(), cancellationToken);
        OnChanged?.Invoke();
    }
}

/// <summary>
/// The people KPIs are filed under — portal users who have been filed against and people added
/// by name (no login). Feeds the pickers (triage "Mark as KPI", the register's filter and
/// re-file modal) alongside the sign-in directory, so someone without a login can be picked
/// again once added.
/// </summary>
public sealed class KpiPeopleReadModel : IReadModelStore<IReadOnlyList<KpiPerson>>
{
    private readonly IQueryClient queries;

    public KpiPeopleReadModel(IQueryClient queries) { this.queries = queries; }

    public IReadOnlyList<KpiPerson>? Current { get; private set; }

    public event Action? OnChanged;

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListKpiPeople(), cancellationToken);
        OnChanged?.Invoke();
    }
}
