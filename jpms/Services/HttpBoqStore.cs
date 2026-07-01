using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Features.Boq;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpBoqStore : IBoqStore
{
    private readonly BoqLinesReadModel readModel;
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    // Projects whose BoQ lines have had a load started — prevents an empty result
    // from re-triggering a fetch on every re-render (see HttpDrawingStore).
    private readonly HashSet<string> requested = new();

    public HttpBoqStore(BoqLinesReadModel readModel, IQueryClient queries, ICommandSender commands)
    {
        this.readModel = readModel;
        this.queries = queries;
        this.commands = commands;
        readModel.OnChanged += () => OnChange?.Invoke();
    }

    public event Action? OnChange;

    public IReadOnlyList<BoqLineItem> LinesFor(string projectId)
    {
        if (requested.Add(projectId)) _ = LoadAsync(projectId);
        return readModel.Current(projectId);
    }

    private async Task LoadAsync(string projectId)
    {
        try { await readModel.RefreshAsync(projectId, CancellationToken.None); }
        catch { requested.Remove(projectId); }
    }

    public BoqLineItem Upsert(BoqLineItem line)
    {
        if (string.IsNullOrEmpty(line.BoqLineItemId)) _ = AddAsync(line);
        else _ = UpdateAsync(line);
        return line;
    }

    public bool Remove(string boqLineItemId)
    {
        _ = commands.SendAsync(new RemoveBoqLine(boqLineItemId), CancellationToken.None);
        return true;
    }

    public decimal TotalFor(string projectId) => LinesFor(projectId).Sum(line => line.LineTotal);

    public BoqSignOff? SignOffFor(string projectId) =>
        queries.AskAsync(new GetBoqSignOffForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public BoqSignOff RecordSignOff(BoqSignOff signOff)
    {
        _ = commands.SendAsync(
            new SignOffBoqForProject(signOff.ProjectId, signOff.SignedOffByEmail, signOff.TenderTotalAtSignOff),
            CancellationToken.None);
        return signOff;
    }

    private async Task AddAsync(BoqLineItem line)
    {
        await commands.SendAsync(
            new AddBoqLine(line.ProjectId, line.Description, line.Unit, line.Quantity, line.RateValue, line.CostCode, line.Discipline),
            CancellationToken.None);
        await readModel.RefreshAsync(line.ProjectId, CancellationToken.None);
    }

    private async Task UpdateAsync(BoqLineItem line)
    {
        await commands.SendAsync(
            new UpdateBoqLine(line.BoqLineItemId, line.Description, line.Unit, line.Quantity, line.RateValue, line.CostCode, line.Discipline),
            CancellationToken.None);
        await readModel.RefreshAsync(line.ProjectId, CancellationToken.None);
    }
}
