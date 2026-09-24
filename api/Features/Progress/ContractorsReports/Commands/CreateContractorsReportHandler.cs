using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Commands;

// Opens the week's report with everything a person would otherwise copy from last week: the
// number, the header fields and standing lines that carry forward, Look Ahead's unstruck items,
// the current valuation as the Valuation No., the Friday after the period as the date of issue,
// Neighbours' default line, and every update in the period selected. Section 4 is deliberately
// NOT carried — it is read from the register every build.
public sealed class CreateContractorsReportHandler : ICommandHandler<CreateContractorsReport, ContractorsReport>
{
    private readonly JpmsContext context;
    public CreateContractorsReportHandler(JpmsContext context) { this.context = context; }

    public async Task<ContractorsReport> HandleAsync(CreateContractorsReport command, CancellationToken cancellationToken)
    {
        var week = ReportingWeek.EndingOn(command.PeriodEnd);
        var project = await context.Projects.AsNoTracking().FirstOrDefaultAsync(row => row.ProjectId == command.ProjectId, cancellationToken)
            ?? throw new InvalidOperationException("Project not found.");
        if (await context.ContractorsReports.AnyAsync(row => row.ProjectId == project.ProjectId && row.PeriodEnd == week.End, cancellationToken))
            throw new InvalidOperationException($"A Contractor's Report for the week ending {week.End:d MMMM yyyy} already exists on {project.Reference}.");

        var previous = await context.ContractorsReports.AsNoTracking()
            .Where(row => row.ProjectId == project.ProjectId && row.PeriodEnd < week.End)
            .OrderByDescending(row => row.PeriodEnd)
            .FirstOrDefaultAsync(cancellationToken);
        var newestNumber = await context.ContractorsReports
            .Where(row => row.ProjectId == project.ProjectId)
            .Select(row => (int?)row.Number)
            .MaxAsync(cancellationToken) ?? 0;

        var now = DateTimeOffset.UtcNow;
        var entity = new ContractorsReportEntity
        {
            ContractorsReportId = ProgressIdentifierFactory.NextContractorsReportId(),
            ProjectId = project.ProjectId,
            Number = command.Number ?? newestNumber + 1,
            PeriodStart = week.Start,
            PeriodEnd = week.End,
            ValuationNumber = await ContractorsReportValuations.CurrentNumberAsync(context, project.ProjectId, cancellationToken) ?? "",
            ProgrammeReference = previous?.ProgrammeReference ?? "",
            PreparedByName = previous?.PreparedByName ?? "",
            IssuedTo = previous?.IssuedTo ?? "",
            DateOfIssue = week.End.AddDays(1),
            LookAheadJson = ContractorsReportJson.Write(CarriedForward(previous)),
            Neighbours = ContractorsReportDefaults.Neighbours,
            HealthAndSafety = previous?.HealthAndSafety ?? "",
            BuildingControlLiaison = previous?.BuildingControlLiaison ?? "",
            BuildingControlContact = previous?.BuildingControlContact ?? "",
            AttendanceJson = ContractorsReportJson.Write(Array.Empty<ContractorsReportAttendance>()),
            SelectedUpdateIdsJson = ContractorsReportJson.Write(await UpdatesInPeriodAsync(project.ProjectId, week, cancellationToken)),
            CreatedByEmail = command.CreatedByEmail,
            CreatedAt = now,
            UpdatedAt = now
        };
        context.ContractorsReports.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private static IReadOnlyList<ContractorsReportLookAheadItem> CarriedForward(ContractorsReportEntity? previous) =>
        previous is null
            ? Array.Empty<ContractorsReportLookAheadItem>()
            : ContractorsReportJson.Read<ContractorsReportLookAheadItem>(previous.LookAheadJson)
                .Where(item => !item.IsDone)
                .ToList();

    private async Task<IReadOnlyList<string>> UpdatesInPeriodAsync(string projectId, ReportingWeek week, CancellationToken cancellationToken) =>
        (await ContractorsReportProgressReader.UpdatesInPeriodAsync(context, projectId, week, cancellationToken))
            .Select(update => update.ProgressUpdateId)
            .ToList();
}
