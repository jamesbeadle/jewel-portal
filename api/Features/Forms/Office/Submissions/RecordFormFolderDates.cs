using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Mapping;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Submissions;

/// <summary>
/// The dates that start a folder's retention clocks: the day the person's engagement ended, and the
/// day a company vehicle came back. Until they are recorded nothing of theirs is ever destroyed — a
/// leaver with no date is reported, not clocked (lib/retention.js). As the dashboard's one leaving
/// date clocked every folder, the engagement's end is also written onto the right-to-work checks and
/// training records made from this folder's forms, wherever theirs is still blank.
/// </summary>
public sealed class RecordFormFolderDatesHandler : ICommandHandler<RecordFormFolderDates, FormFolder>
{
    private readonly JpmsContext context;

    public RecordFormFolderDatesHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<FormFolder> HandleAsync(RecordFormFolderDates command, CancellationToken cancellationToken)
    {
        var folder = await context.FormFolders.FirstOrDefaultAsync(row => row.FormFolderId == command.FormFolderId, cancellationToken)
            ?? throw new InvalidOperationException("That folder no longer exists.");
        context.AuditEvents.Add(DatesRecord(folder, command));
        folder.EngagementEndedOn = command.EngagementEndedOn;
        folder.VehicleReturnedOn = command.VehicleReturnedOn;
        if (command.EngagementEndedOn is { } endedOn) await EndTheRegistersAsync(folder.FormFolderId, endedOn, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        var submissions = await context.FormSubmissions
            .CountAsync(row => row.FormFolderId == folder.FormFolderId && row.DestroyedAt == null, cancellationToken);
        return folder.ToModel(submissions);
    }

    private static AuditEventEntity DatesRecord(FormFolderEntity folder, RecordFormFolderDates command) =>
        FormAuditRecords.Of(AuditEventType.FormFolderDatesRecorded, folder.FormFolderId, command.RecordedByEmail,
            $"Engagement ended {Dated(folder.EngagementEndedOn)} → {Dated(command.EngagementEndedOn)}; "
            + $"vehicle returned {Dated(folder.VehicleReturnedOn)} → {Dated(command.VehicleReturnedOn)}");

    private static string Dated(DateOnly? date) => date is { } day ? FormDates.Write(day) : "not recorded";

    private async Task EndTheRegistersAsync(string formFolderId, DateOnly endedOn, CancellationToken cancellationToken)
    {
        var formIds = context.FormSubmissions.Where(row => row.FormFolderId == formFolderId).Select(row => (string?)row.FormSubmissionId);
        var checks = await context.RightToWorkChecks
            .Where(row => row.EngagementEndedOn == null && formIds.Contains(row.FormSubmissionId)).ToListAsync(cancellationToken);
        foreach (var check in checks) check.EngagementEndedOn = endedOn;
        var records = await context.TrainingRecords
            .Where(row => row.EndedOn == null && formIds.Contains(row.FormSubmissionId)).ToListAsync(cancellationToken);
        foreach (var record in records) record.EndedOn = endedOn;
    }
}

public sealed class RecordFormFolderDatesAuthorisation
{
    public bool Allows(SignedInUser user, RecordFormFolderDates command) => FormRoleSets.Office.IncludesAny(user.Roles);
}

public sealed class RecordFormFolderDatesValidation
{
    public ValidationOutcome Check(RecordFormFolderDates command)
    {
        var errors = new List<string>();
        var today = FormClock.Today();
        if (string.IsNullOrWhiteSpace(command.FormFolderId)) errors.Add("Which folder?");
        if (command.EngagementEndedOn > today) errors.Add("Record the day the engagement ended once it has — not a day still to come.");
        if (command.VehicleReturnedOn > today) errors.Add("Record the day the vehicle came back once it has — not a day still to come.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
