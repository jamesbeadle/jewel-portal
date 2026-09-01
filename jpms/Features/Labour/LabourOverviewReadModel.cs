using Jewel.JPMS.Contracts.Labour;

namespace Jewel.JPMS.Features.Labour;

/// <summary>
/// The company-wide Labour overview, cached per month
/// (docs/Labour-Overview-Forecast-and-Xero-Mapping-Scope.md §4).
/// </summary>
public sealed class LabourOverviewReadModel
{
    private readonly IQueryClient queries;
    private readonly Dictionary<string, LabourOverviewSnapshot> snapshotsByMonth = new();

    public LabourOverviewReadModel(IQueryClient queries) { this.queries = queries; }

    public event Action? OnChanged;

    private static string KeyOf(int year, int month) => $"{year:0000}-{month:00}";

    public LabourOverviewSnapshot? Current(int year, int month) =>
        snapshotsByMonth.TryGetValue(KeyOf(year, month), out var snapshot) ? snapshot : null;

    /// <summary>True once this month's snapshot has landed. Current(...) answers null until
    /// then — gate every figure, grid and empty state on this first.</summary>
    public bool LoadedFor(int year, int month) => snapshotsByMonth.ContainsKey(KeyOf(year, month));

    public async Task RefreshAsync(int year, int month, CancellationToken cancellationToken)
    {
        snapshotsByMonth[KeyOf(year, month)] = await queries.AskAsync(new GetLabourOverview(year, month), cancellationToken);
        OnChanged?.Invoke();
    }
}
