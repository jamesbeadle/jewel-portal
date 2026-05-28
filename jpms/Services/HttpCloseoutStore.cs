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

    public HttpCloseoutStore(DefectsReadModel defectsReadModel, IQueryClient queries, ICommandSender commands)
    {
        this.defectsReadModel = defectsReadModel;
        this.queries = queries;
        this.commands = commands;
        defectsReadModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<Defect> DefectsFor(string projectId)
    {
        if (defectsReadModel.Current(projectId).Count == 0) _ = defectsReadModel.RefreshAsync(projectId, CancellationToken.None);
        return defectsReadModel.Current(projectId);
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
        queries.AskAsync(new GetSettlementForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public SettlementRecord SaveSettlement(SettlementRecord settlement)
    {
        _ = commands.SendAsync(new AgreeSettlement(settlement.ProjectId, settlement.FinalContractValue, settlement.FinalCost, settlement.FinalMargin, settlement.IsClientSigned), CancellationToken.None);
        return settlement;
    }

    public VatAnalysis? VatFor(string projectId) =>
        queries.AskAsync(new GetVatAnalysisForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public VatAnalysis SaveVat(VatAnalysis analysis)
    {
        _ = commands.SendAsync(new AgreeVatAnalysis(analysis.ProjectId, analysis.ZeroRatedAmount, analysis.StandardRatedAmount, analysis.Notes, analysis.IsClientConfirmed, analysis.IsArchitectConfirmed), CancellationToken.None);
        return analysis;
    }

    public RetentionRelease? RetentionFor(string projectId) => null;

    public RetentionRelease SaveRetention(RetentionRelease release)
    {
        _ = commands.SendAsync(new ReleaseRetention(release.ProjectId, release.Amount, release.IsPublishedDownstream), CancellationToken.None);
        return release;
    }
}
