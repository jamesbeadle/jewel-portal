using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Api.Features.Labour.Queries;
using Jewel.JPMS.Contracts.Labour;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static Jewel.JPMS.Tests.MyDayWeeksSeed;

namespace Jewel.JPMS.Tests;

/// <summary>The week planner (1 Oct 2026, Jeremy's ask): every operative in by default, marked not
/// in for the days they asked off, the forecast at the day rate, and the actuals beside it once
/// logged.</summary>
public sealed class LabourWeekPlanTests
{
    [Fact]
    public async Task EveryoneIsIn_untilMarkedNotIn_andTheForecastFollows()
    {
        await using var context = await SeededAsync();
        var planner = new PlanWorkerDaysHandler(context);
        var monday = LastMonday;

        var before = await new LabourWeekPlanHandler(context).HandleAsync(new GetLabourWeekPlan(monday.AddDays(2)), CancellationToken.None);
        var jack = Assert.Single(before.Workers);
        Assert.Equal(monday, before.WeekStart);
        Assert.Equal(5m, jack.PlannedDays);
        Assert.Equal(1000m, jack.PlannedCost);

        await planner.HandleAsync(new PlanWorkerDays(WorkerId, new[] { monday, monday.AddDays(1) }, IsIn: false), Director, CancellationToken.None);
        var after = await new LabourWeekPlanHandler(context).HandleAsync(new GetLabourWeekPlan(monday), CancellationToken.None);
        var planned = Assert.Single(after.Workers);
        Assert.Equal(3m, planned.PlannedDays);
        Assert.Equal(600m, after.PlannedCost);
        Assert.Equal(AbsenceKind.Holiday, planned.Days[0].Absence);
        Assert.Equal(2, await context.WorkerAbsences.CountAsync());

        await planner.HandleAsync(new PlanWorkerDays(WorkerId, new[] { monday }, IsIn: true), Director, CancellationToken.None);
        Assert.Equal(1, await context.WorkerAbsences.CountAsync());
    }

    [Fact]
    public async Task ADayAlreadyLogged_cannotBePlannedOff_andShowsItsActual()
    {
        await using var context = await SeededAsync();
        await LogDaysAsync(context, LastMonday);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new PlanWorkerDaysHandler(context).HandleAsync(new PlanWorkerDays(WorkerId, new[] { LastMonday }, IsIn: false), Director, CancellationToken.None));

        var plan = await new LabourWeekPlanHandler(context).HandleAsync(new GetLabourWeekPlan(LastMonday), CancellationToken.None);
        var day = plan.Workers.Single().Days[0];
        Assert.True(day.IsLogged);
        Assert.Equal(8m, day.LoggedHours);
        Assert.Equal(TimesheetStatus.Submitted, day.Status);
        Assert.Equal(200m, day.ActualCost);
        Assert.Equal(200m, plan.ActualCost);
    }
}
