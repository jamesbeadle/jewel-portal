using Jewel.JPMS.Contracts.CommercialInputs;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed class HttpCommercialInputsStore : ICommercialInputsStore
{
    private readonly IQueryClient queries;
    private readonly ICommandSender commands;

    // Cache the async list queries so render-time reads never block on async (which deadlocks
    // on WebAssembly). Mutations invalidate the affected project so the next read refetches.
    private readonly AsyncQueryCache<string, IReadOnlyList<Daywork>> dayworks;
    private readonly AsyncQueryCache<string, IReadOnlyList<ContraCharge>> contraCharges;
    private readonly AsyncQueryCache<string, IReadOnlyList<SubcontractorRetention>> retentions;

    public HttpCommercialInputsStore(IQueryClient queries, ICommandSender commands)
    {
        this.queries = queries;
        this.commands = commands;
        dayworks = new((id, ct) => queries.AskAsync(new ListDayworksForProject(id), ct), () => OnChange?.Invoke());
        contraCharges = new((id, ct) => queries.AskAsync(new ListContraChargesForProject(id), ct), () => OnChange?.Invoke());
        retentions = new((id, ct) => queries.AskAsync(new ListSubcontractorRetentionsForProject(id), ct), () => OnChange?.Invoke());
    }

    public event Action? OnChange;

    public IReadOnlyList<Daywork> DayworksFor(string projectId) =>
        dayworks.Get(projectId, Array.Empty<Daywork>());

    public Daywork LogDaywork(Daywork daywork)
    {
        _ = SendThenInvalidate(
            new LogDaywork(daywork.ProjectId, daywork.WorkedOn, daywork.SubcontractorReference, daywork.Description, daywork.InstructedBy, daywork.Hours, daywork.HourlyRate, daywork.LabourCost, daywork.PlantCost, daywork.MaterialsCost, daywork.UpliftPercent, daywork.ChargeableAmount),
            dayworks, daywork.ProjectId);
        return daywork;
    }

    public IReadOnlyList<ContraCharge> ContraChargesFor(string projectId) =>
        contraCharges.Get(projectId, Array.Empty<ContraCharge>());

    public ContraCharge RecordContraCharge(ContraCharge contraCharge)
    {
        _ = SendThenInvalidate(
            new RecordContraCharge(contraCharge.ProjectId, contraCharge.SubcontractorReference, contraCharge.RaisedOn, contraCharge.Description, contraCharge.Category, contraCharge.Amount, contraCharge.Status, contraCharge.RecoveredAmount),
            contraCharges, contraCharge.ProjectId);
        return contraCharge;
    }

    public IReadOnlyList<SubcontractorRetention> RetentionsFor(string projectId) =>
        retentions.Get(projectId, Array.Empty<SubcontractorRetention>());

    public SubcontractorRetention RecordRetention(SubcontractorRetention retention)
    {
        _ = SendThenInvalidate(
            new RecordSubcontractorRetention(retention.ProjectId, retention.SubcontractorReference, retention.CertifiedAmount, retention.RetentionPercent, retention.FirstReleasedAmount, retention.FinalReleasedAmount),
            retentions, retention.ProjectId);
        return retention;
    }

    // Await the command, then invalidate the affected cache key so the refetch (and its change
    // notification) carries the new data. Invalidating before the command completes would refetch
    // stale rows.
    private async Task SendThenInvalidate<TResult, TValue>(
        Jewel.JPMS.Contracts.Cqrs.ICommand<TResult> command,
        AsyncQueryCache<string, TValue> cache, string key)
    {
        await commands.SendAsync(command, CancellationToken.None);
        cache.Invalidate(key);
    }
}
