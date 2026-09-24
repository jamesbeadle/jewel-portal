using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Commands;

public sealed class UpdateContractorsReportHandler : ICommandHandler<UpdateContractorsReport, ContractorsReport>
{
    private readonly JpmsContext context;
    public UpdateContractorsReportHandler(JpmsContext context) { this.context = context; }

    public async Task<ContractorsReport> HandleAsync(UpdateContractorsReport command, CancellationToken cancellationToken)
    {
        var entity = await context.ContractorsReports.FirstOrDefaultAsync(row => row.ContractorsReportId == command.ContractorsReportId, cancellationToken)
            ?? throw new InvalidOperationException("Contractor's Report not found.");

        entity.ValuationNumber = command.ValuationNumber.Trim();
        entity.ProgrammeReference = command.ProgrammeReference.Trim();
        entity.PreparedByName = command.PreparedByName.Trim();
        entity.IssuedTo = command.IssuedTo.Trim();
        entity.DateOfIssue = command.DateOfIssue;
        entity.LookAheadJson = ContractorsReportJson.Write(KeptLines(command.LookAhead));
        entity.Neighbours = command.Neighbours.Trim();
        entity.HealthAndSafety = command.HealthAndSafety.Trim();
        entity.BuildingControlLiaison = command.BuildingControlLiaison.Trim();
        entity.AttendanceJson = ContractorsReportJson.Write(command.Attendance);
        entity.SelectedUpdateIdsJson = ContractorsReportJson.Write(command.SelectedUpdateIds.Distinct().ToList());
        if (command.BuildingControlContact is { } contact) entity.BuildingControlContact = contact.Trim();
        if (command.ExcludedPhotoIds is { } excluded) entity.ExcludedPhotoIdsJson = ExcludedJson(excluded);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private static string ExcludedJson(IReadOnlyList<string> photoIds) => ContractorsReportJson.Write(photoIds.Distinct().ToList());

    private static IReadOnlyList<ContractorsReportLookAheadItem> KeptLines(IReadOnlyList<ContractorsReportLookAheadItem> lookAhead) =>
        lookAhead
            .Where(item => !string.IsNullOrWhiteSpace(item.Text))
            .Select(item => item with { Text = item.Text.Trim() })
            .ToList();
}
