using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed partial class HttpCvrStore
{
    public IReadOnlyList<ForecastComponent> ForecastComponentsFor(string projectId) =>
        queries.AskAsync(new ListForecastComponentsForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public ForecastComponent SaveForecastComponent(ForecastComponent component)
    {
        _ = commands.SendAsync(
            new RecordForecastComponent(component.ProjectId, component.PackageName, component.CostIncurred, component.CostCommitted, component.QsAccrualAmount, component.PrelimForecast, component.CostToComplete),
            CancellationToken.None);
        OnChange?.Invoke();
        return component;
    }

    public IReadOnlyList<QsAccrual> AccrualsFor(string projectId) =>
        queries.AskAsync(new ListQsAccrualsForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public QsAccrual SaveAccrual(QsAccrual accrual)
    {
        if (string.IsNullOrEmpty(accrual.QsAccrualId))
            _ = commands.SendAsync(new RecordQsAccrual(accrual.ProjectId, accrual.Category, accrual.Description, accrual.AddAmount, accrual.OmitAmount, accrual.LiabilityAmount, accrual.SignedOffByEmail), CancellationToken.None);
        else
            _ = commands.SendAsync(new UpdateQsAccrual(accrual.QsAccrualId, accrual.Category, accrual.Description, accrual.AddAmount, accrual.OmitAmount, accrual.LiabilityAmount, accrual.SignedOffByEmail), CancellationToken.None);
        OnChange?.Invoke();
        return accrual;
    }

    public IReadOnlyList<PrelimItem> PrelimsFor(string projectId) =>
        queries.AskAsync(new ListPrelimItemsForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public IReadOnlyList<PrelimForecastEntry> PrelimEntriesFor(string prelimItemId) =>
        queries.AskAsync(new ListPrelimEntriesForItem(prelimItemId), CancellationToken.None).GetAwaiter().GetResult();

    public PrelimForecastEntry SavePrelimForecast(string projectId, string prelimDescription, PrelimForecastEntry entry)
    {
        _ = commands.SendAsync(
            new RecordPrelimForecastForWeek(projectId, prelimDescription, entry.WeekNumber, entry.TenderedAmount, entry.ActualAmount, entry.ForecastAmount),
            CancellationToken.None);
        OnChange?.Invoke();
        return entry;
    }

    public IReadOnlyList<Eot> EotsFor(string projectId) =>
        queries.AskAsync(new ListEotsForProject(projectId), CancellationToken.None).GetAwaiter().GetResult();

    public Eot SaveEot(Eot eot)
    {
        if (string.IsNullOrEmpty(eot.EotId))
            _ = commands.SendAsync(new GrantEot(eot.ProjectId, eot.Reason, eot.DaysGranted, eot.CommercialRecovery), CancellationToken.None);
        else
            _ = commands.SendAsync(new UpdateEot(eot.EotId, eot.Reason, eot.DaysGranted, eot.CommercialRecovery), CancellationToken.None);
        OnChange?.Invoke();
        return eot;
    }
}
