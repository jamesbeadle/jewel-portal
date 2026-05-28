using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Cvr;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Cvr.Commands;

public sealed class RecordPrelimForecastForWeekHandler : ICommandHandler<RecordPrelimForecastForWeek, PrelimForecastEntry>
{
    private readonly JpmsContext context;

    public RecordPrelimForecastForWeekHandler(JpmsContext context) { this.context = context; }

    public async Task<PrelimForecastEntry> HandleAsync(RecordPrelimForecastForWeek command, CancellationToken cancellationToken)
    {
        var item = await context.PrelimItems.FirstOrDefaultAsync(
            prelim => prelim.ProjectId == command.ProjectId && prelim.Description == command.PrelimDescription, cancellationToken)
            ?? AddNewPrelimItem(command);

        var entry = await context.PrelimForecastEntries.FirstOrDefaultAsync(
            forecast => forecast.PrelimItemId == item.PrelimItemId && forecast.WeekNumber == command.WeekNumber, cancellationToken)
            ?? AddNewForecastEntry(item.PrelimItemId, command.WeekNumber);

        entry.TenderedAmount = command.TenderedAmount;
        entry.ActualAmount = command.ActualAmount;
        entry.ForecastAmount = command.ForecastAmount;

        await context.SaveChangesAsync(cancellationToken);
        return entry.ToModel();
    }

    private PrelimItemEntity AddNewPrelimItem(RecordPrelimForecastForWeek command)
    {
        var item = new PrelimItemEntity
        {
            PrelimItemId = CvrIdentifierFactory.NextPrelimItemId(),
            ProjectId = command.ProjectId,
            Description = command.PrelimDescription
        };
        context.PrelimItems.Add(item);
        return item;
    }

    private PrelimForecastEntryEntity AddNewForecastEntry(string prelimItemId, int weekNumber)
    {
        var entry = new PrelimForecastEntryEntity
        {
            PrelimForecastEntryId = CvrIdentifierFactory.NextPrelimForecastEntryId(),
            PrelimItemId = prelimItemId,
            WeekNumber = weekNumber
        };
        context.PrelimForecastEntries.Add(entry);
        return entry;
    }
}
