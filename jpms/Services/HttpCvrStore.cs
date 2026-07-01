using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Cvr;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed partial class HttpCvrStore : ICvrStore
{
    private readonly CvrSnapshotsReadModel snapshotsReadModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    // Projects whose snapshots have had a load started — prevents an empty result
    // from re-triggering a fetch on every re-render (see HttpDrawingStore).
    private readonly HashSet<string> requested = new();

    // Caches for the async list queries so render-time reads never block on async (which
    // deadlocks on WebAssembly). Declared here; also used by the HttpCvrStore.Depth partial.
    private readonly AsyncQueryCache<string, IReadOnlyList<CvrPackageRow>> packages;
    private readonly AsyncQueryCache<string, IReadOnlyList<ForecastComponent>> forecastComponents;
    private readonly AsyncQueryCache<string, IReadOnlyList<QsAccrual>> accruals;
    private readonly AsyncQueryCache<string, IReadOnlyList<PrelimItem>> prelims;
    private readonly AsyncQueryCache<string, IReadOnlyList<PrelimForecastEntry>> prelimEntries;
    private readonly AsyncQueryCache<string, IReadOnlyList<Eot>> eots;

    public HttpCvrStore(CvrSnapshotsReadModel snapshotsReadModel, IQueryClient queries, ICommandSender commands)
    {
        this.snapshotsReadModel = snapshotsReadModel;
        this.queries = queries;
        this.commands = commands;
        snapshotsReadModel.OnChanged += () => OnChange?.Invoke();

        Action notify = () => OnChange?.Invoke();
        packages = new((id, ct) => queries.AskAsync(new ListCvrPackagesForProject(id), ct), notify);
        forecastComponents = new((id, ct) => queries.AskAsync(new ListForecastComponentsForProject(id), ct), notify);
        accruals = new((id, ct) => queries.AskAsync(new ListQsAccrualsForProject(id), ct), notify);
        prelims = new((id, ct) => queries.AskAsync(new ListPrelimItemsForProject(id), ct), notify);
        prelimEntries = new((id, ct) => queries.AskAsync(new ListPrelimEntriesForItem(id), ct), notify);
        eots = new((id, ct) => queries.AskAsync(new ListEotsForProject(id), ct), notify);
    }

    public event Action? OnChange;

    public IReadOnlyList<CvrSnapshot> SnapshotsFor(string projectId)
    {
        if (requested.Add(projectId)) _ = LoadAsync(projectId);
        return snapshotsReadModel.Current(projectId);
    }

    private async Task LoadAsync(string projectId)
    {
        try { await snapshotsReadModel.RefreshAsync(projectId, CancellationToken.None); }
        catch { requested.Remove(projectId); }
    }

    public CvrSnapshot? LatestSnapshot(string projectId) =>
        SnapshotsFor(projectId).FirstOrDefault();

    public CvrSnapshot CaptureSnapshot(CvrSnapshot snapshot)
    {
        _ = CaptureSnapshotAsync(snapshot);
        return snapshot;
    }

    private async Task CaptureSnapshotAsync(CvrSnapshot snapshot)
    {
        await commands.SendAsync(
            new CaptureCvrSnapshot(snapshot.ProjectId, snapshot.TenderValue, snapshot.ForecastFinalCost, snapshot.ForecastFinalValue, snapshot.WeeksAheadOrBehind),
            CancellationToken.None);
        await snapshotsReadModel.RefreshAsync(snapshot.ProjectId, CancellationToken.None);
    }

    public IReadOnlyList<CvrPackageRow> PackagesFor(string projectId) =>
        packages.Get(projectId, Array.Empty<CvrPackageRow>());

    public CvrPackageRow SavePackageRow(CvrPackageRow row)
    {
        _ = SendThenInvalidate(
            new RecordCvrPackageRow(row.ProjectId, row.PackageName, row.OrderCost, row.OrderValue, row.VariationCost, row.VariationValue),
            packages, row.ProjectId);
        return row;
    }

    // Await the command, then invalidate the affected cache key so the refetch (and its change
    // notification) carries the new data. Shared with the HttpCvrStore.Depth partial.
    private async Task SendThenInvalidate<TResult, TValue>(
        Jewel.JPMS.Contracts.Cqrs.ICommand<TResult> command,
        AsyncQueryCache<string, TValue> cache, string key)
    {
        await commands.SendAsync(command, CancellationToken.None);
        cache.Invalidate(key);
    }
}
