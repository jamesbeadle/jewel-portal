using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Features.Labour;

/// <summary>Per-worker monthly settlement schedules, cached per month (scope §6).</summary>
public sealed class SettlementSchedulesReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, SettlementScheduleSnapshot> snapshotsByMonth = new();

    public SettlementSchedulesReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    private static string KeyOf(int year, int month) => $"{year:0000}-{month:00}";

    public SettlementScheduleSnapshot? Current(int year, int month) =>
        snapshotsByMonth.TryGetValue(KeyOf(year, month), out var snapshot) ? snapshot : null;

    /// <summary>True once this month's schedules have landed. Current(...) answers null until
    /// then — gate every figure, verdict and empty state on this first.</summary>
    public bool LoadedFor(int year, int month) => snapshotsByMonth.ContainsKey(KeyOf(year, month));

    public async Task RefreshAsync(int year, int month, CancellationToken cancellationToken)
    {
        snapshotsByMonth[KeyOf(year, month)] = await queries.AskAsync(new GetSettlementSchedules(year, month), cancellationToken);
        OnChanged?.Invoke();
    }
}

/// <summary>The effective-dated Xero mapping tables (scope §3). Single-key cache.</summary>
public sealed class XeroMappingsReadModel
{
    private readonly IQueryClient queries;

    public XeroMappingsReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    public XeroMappingsSnapshot? Current { get; private set; }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        Current = await queries.AskAsync(new ListXeroMappings(), cancellationToken);
        OnChanged?.Invoke();
    }
}
