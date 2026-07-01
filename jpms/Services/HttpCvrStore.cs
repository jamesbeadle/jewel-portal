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

    public HttpCvrStore(CvrSnapshotsReadModel snapshotsReadModel, IQueryClient queries, ICommandSender commands)
    {
        this.snapshotsReadModel = snapshotsReadModel;
        this.queries = queries;
        this.commands = commands;
        snapshotsReadModel.OnChanged += () => OnChange?.Invoke();
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
        queries.AskAsync(new ListCvrPackagesForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public CvrPackageRow SavePackageRow(CvrPackageRow row)
    {
        _ = SendThenNotify(new RecordCvrPackageRow(row.ProjectId, row.PackageName, row.OrderCost, row.OrderValue, row.VariationCost, row.VariationValue));
        return row;
    }

    private async Task SendThenNotify<TResult>(Jewel.JPMS.Contracts.Cqrs.ICommand<TResult> command)
    {
        await commands.SendAsync(command, CancellationToken.None);
        OnChange?.Invoke();
    }
}
