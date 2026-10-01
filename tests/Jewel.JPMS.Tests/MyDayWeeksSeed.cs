using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Labour;
using Jewel.JPMS.Api.Features.Labour.Commands;
using Jewel.JPMS.Api.Features.Labour.Queries;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jewel.JPMS.Tests;

/// <summary>One worker on one site, with last week's days written straight to the tables, for the
/// tests of My day's weeks, the month, the week sent for review and the week planner.</summary>
internal static class MyDayWeeksSeed
{
    public const string Email = "jack@example.com";
    public const string WorkerId = "w-jack";
    public const string Project = "p-abbot-road";
    public const string Code = "LAB";
    public const string Director = "jeremy@jewelbb.co.uk";

    public static DateTimeOffset LastMonday => LabourWeeks.MondayOf(SiteClock.Today().AddDays(-LabourWeeks.DaysInWeek));

    public static async Task<JpmsContext> SeededAsync()
    {
        var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"my-weeks-{Guid.NewGuid():N}").Options);
        context.Workers.Add(new WorkerEntity { WorkerId = WorkerId, Name = "Jack Eastly", ContactEmail = Email, HourlyRate = 25m, IsActive = true });
        context.Projects.Add(new ProjectEntity { ProjectId = Project, Name = "Abbot Road" });
        context.ProjectWorkerAssignments.Add(new ProjectWorkerAssignmentEntity { ProjectWorkerAssignmentId = "a-1", ProjectId = Project, WorkerId = WorkerId, IsActive = true });
        context.CostCenters.Add(new CostCenterEntity { CostCenterId = "cc-lab", Code = Code, Name = "Labour", IsActive = true });
        context.CostCodeBudgets.Add(new CostCodeBudgetEntity { CostCodeBudgetId = "b-lab", ProjectId = Project, CostCode = Code, AllocatedAmount = 10000m });
        await context.SaveChangesAsync();
        return context;
    }

    public static async Task LogDaysAsync(JpmsContext context, params DateTimeOffset[] days)
    {
        foreach (var day in days)
        {
            context.Timesheets.Add(new TimesheetEntity
            {
                TimesheetId = $"t-{day:yyyyMMdd}", ProjectId = Project, PersonEmail = Email, WorkerId = WorkerId,
                WorkedOn = day, Hours = 8m, CostCode = Code, Status = (int)TimesheetStatus.Submitted,
            });
            context.ProgressUpdates.Add(new ProgressUpdateEntity
            {
                ProgressUpdateId = $"n-{day:yyyyMMdd}", ProjectId = Project, Title = "Daily log — Jack Eastly",
                Description = "Second fix.", WorkDate = day, CreatedByEmail = Email, CreatedAt = day,
            });
        }
        await context.SaveChangesAsync();
    }

    public static async Task RecordOffAsync(JpmsContext context, DateTimeOffset day)
    {
        context.ProgressUpdates.Add(new ProgressUpdateEntity
        {
            ProgressUpdateId = $"off-{day:yyyyMMdd}", ProjectId = Project, Title = "Off — Jack Eastly",
            Description = "Rained off.", WorkDate = day, CreatedByEmail = Email, CreatedAt = day,
        });
        await context.SaveChangesAsync();
    }

    public static MyDayProjects DayProjects(JpmsContext context) => new(context, new MyDayCostCodes(context), new MyDayNotesToday(context));

    public static GetMyLabourWeekHandler WeekHandler(JpmsContext context) => new(context, DayProjects(context), new MyDayDays(context));

    public static GetMyLabourMonthHandler MonthHandler(JpmsContext context) => new(context, DayProjects(context), new MyDayDays(context));

    public static MySubmitWeekHandler SubmitHandler(JpmsContext context) => new(context, WeekHandler(context));

    public static WorkerWeekSubmissionReader Reader(JpmsContext context) => new(context, new MyDayDays(context));

    public static SignOffWorkerWeekHandler SignOffHandler(JpmsContext context)
    {
        var audit = new Jewel.JPMS.Api.Features.Audit.AuditTrail(context, new Jewel.JPMS.Api.Features.Audit.AuditActor { Email = Director },
            NullLogger<Jewel.JPMS.Api.Features.Audit.AuditTrail>.Instance);
        return new SignOffWorkerWeekHandler(context, new ApproveTimesheetsHandler(context, audit), new SignOffLabourWeekHandler(context), Reader(context));
    }

    public static SendBackWorkerWeekHandler SendBackHandler(JpmsContext context) => new(context, Reader(context));
}
