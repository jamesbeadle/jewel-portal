using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Closeout;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpCloseoutStore : ICloseoutStore
{
    private readonly DefectsReadModel defectsReadModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    // Projects whose defects have had a load started — prevents an empty result
    // from re-triggering a fetch on every re-render (see HttpDrawingStore).
    private readonly HashSet<string> requested = new();

    // Single-value project lookups, cached so render-time reads never block on async
    // (which deadlocks on WebAssembly). Mutations invalidate the project.
    private readonly AsyncQueryCache<string, SettlementRecord?> settlements;
    private readonly AsyncQueryCache<string, VatAnalysis?> vatAnalyses;
    private readonly AsyncQueryCache<string, RetentionRelease?> retentionReleases;

    public HttpCloseoutStore(DefectsReadModel defectsReadModel, IQueryClient queries, ICommandSender commands)
    {
        this.defectsReadModel = defectsReadModel;
        this.queries = queries;
        this.commands = commands;
        defectsReadModel.OnChanged += () => OnChange?.Invoke();

        Action notify = () => OnChange?.Invoke();
        settlements = new((id, ct) => queries.AskAsync(new GetSettlementForProject(id), ct), notify);
        vatAnalyses = new((id, ct) => queries.AskAsync(new GetVatAnalysisForProject(id), ct), notify);
        retentionReleases = new((id, ct) => queries.AskAsync(new GetRetentionForProject(id), ct), notify);
    }

    public event Action? OnChange;

    public IReadOnlyList<Defect> DefectsFor(string projectId)
    {
        if (requested.Add(projectId)) _ = LoadAsync(projectId);
        return defectsReadModel.Current(projectId);
    }

    private async Task LoadAsync(string projectId)
    {
        try { await defectsReadModel.RefreshAsync(projectId, CancellationToken.None); }
        catch { requested.Remove(projectId); }
    }

    public Defect SaveDefect(Defect defect)
    {
        if (string.IsNullOrEmpty(defect.DefectId))
            _ = commands.SendAsync(new RaiseDefect(defect.ProjectId, defect.Description, defect.Location, defect.AssignedToEmail), CancellationToken.None);
        else
            _ = commands.SendAsync(new UpdateDefect(defect.DefectId, defect.Description, defect.Location, defect.AssignedToEmail, defect.Status), CancellationToken.None);
        return defect;
    }

    public SettlementRecord? SettlementFor(string projectId) =>
        settlements.Get(projectId, null);

    public SettlementRecord SaveSettlement(SettlementRecord settlement)
    {
        _ = SendThenInvalidate(
            new AgreeSettlement(settlement.ProjectId, settlement.FinalContractValue, settlement.FinalCost, settlement.FinalMargin, settlement.IsClientSigned),
            settlements, settlement.ProjectId);
        return settlement;
    }

    public VatAnalysis? VatFor(string projectId) =>
        vatAnalyses.Get(projectId, null);

    public VatAnalysis SaveVat(VatAnalysis analysis)
    {
        _ = SendThenInvalidate(
            new AgreeVatAnalysis(analysis.ProjectId, analysis.ZeroRatedAmount, analysis.StandardRatedAmount, analysis.Notes, analysis.IsClientConfirmed, analysis.IsArchitectConfirmed),
            vatAnalyses, analysis.ProjectId);
        return analysis;
    }

    public RetentionRelease? RetentionFor(string projectId) =>
        retentionReleases.Get(projectId, null);

    public RetentionRelease SaveRetention(RetentionRelease release)
    {
        _ = SendThenInvalidate(
            new ReleaseRetention(release.ProjectId, release.Amount, release.IsPublishedDownstream),
            retentionReleases, release.ProjectId);
        return release;
    }

    // Await the command, then invalidate the affected cache key so the refetch (and its change
    // notification) carries the new data.
    private async Task SendThenInvalidate<TResult, TValue>(
        Jewel.JPMS.Contracts.Cqrs.ICommand<TResult> command,
        AsyncQueryCache<string, TValue> cache, string key)
    {
        await commands.SendAsync(command, CancellationToken.None);
        cache.Invalidate(key);
    }
}
