using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed partial class HttpCvrStore
{
    public IReadOnlyList<ForecastComponent> ForecastComponentsFor(string projectId) =>
        forecastComponents.Get(projectId, Array.Empty<ForecastComponent>());

    public ForecastComponent SaveForecastComponent(ForecastComponent component)
    {
        _ = SendThenInvalidate(
            new RecordForecastComponent(component.ProjectId, component.PackageName, component.CostIncurred, component.CostCommitted, component.QsAccrualAmount, component.PrelimForecast, component.CostToComplete),
            forecastComponents, component.ProjectId);
        return component;
    }

    public IReadOnlyList<QsAccrual> AccrualsFor(string projectId) =>
        accruals.Get(projectId, Array.Empty<QsAccrual>());

    public QsAccrual SaveAccrual(QsAccrual accrual)
    {
        if (string.IsNullOrEmpty(accrual.QsAccrualId))
            _ = SendThenInvalidate(
                new RecordQsAccrual(accrual.ProjectId, accrual.Category, accrual.Description, accrual.AddAmount, accrual.OmitAmount, accrual.LiabilityAmount, accrual.SignedOffByEmail),
                accruals, accrual.ProjectId);
        else
            _ = SendThenInvalidate(
                new UpdateQsAccrual(accrual.QsAccrualId, accrual.Category, accrual.Description, accrual.AddAmount, accrual.OmitAmount, accrual.LiabilityAmount, accrual.SignedOffByEmail),
                accruals, accrual.ProjectId);
        return accrual;
    }

    public IReadOnlyList<PrelimItem> PrelimsFor(string projectId) =>
        prelims.Get(projectId, Array.Empty<PrelimItem>());

    public IReadOnlyList<PrelimForecastEntry> PrelimEntriesFor(string prelimItemId) =>
        prelimEntries.Get(prelimItemId, Array.Empty<PrelimForecastEntry>());

    public PrelimForecastEntry SavePrelimForecast(string projectId, string prelimDescription, PrelimForecastEntry entry)
    {
        _ = SavePrelimForecastAsync(projectId, prelimDescription, entry);
        return entry;
    }

    private async Task SavePrelimForecastAsync(string projectId, string prelimDescription, PrelimForecastEntry entry)
    {
        await commands.SendAsync(
            new RecordPrelimForecastForWeek(projectId, prelimDescription, entry.WeekNumber, entry.TenderedAmount, entry.ActualAmount, entry.ForecastAmount),
            CancellationToken.None);
        // A prelim entry may create the prelim item too; refresh the project's items, and drop all
        // cached entry lists (the affected item id isn't known here) so they refetch on next read.
        prelims.Invalidate(projectId);
        prelimEntries.InvalidateAll();
    }

    public IReadOnlyList<Eot> EotsFor(string projectId) =>
        eots.Get(projectId, Array.Empty<Eot>());

    public Eot SaveEot(Eot eot)
    {
        if (string.IsNullOrEmpty(eot.EotId))
            _ = SendThenInvalidate(new GrantEot(eot.ProjectId, eot.Reason, eot.DaysGranted, eot.CommercialRecovery), eots, eot.ProjectId);
        else
            _ = SendThenInvalidate(new UpdateEot(eot.EotId, eot.Reason, eot.DaysGranted, eot.CommercialRecovery), eots, eot.ProjectId);
        return eot;
    }
}
