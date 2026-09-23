using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Mapping;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Registers;

/// <summary>
/// The checker's record (lib/rtw.js), which is what gives the statutory excuse — so it is made in the
/// office against a named person and a date, never by the public form. A pass cannot be half true,
/// the date is never back- or forward-dated, and time-limited permission is followed up ten weeks
/// before it runs out. Recorded from a Right to Work form, that form is handled.
/// </summary>
public sealed class SaveRightToWorkCheckHandler : ICommandHandler<SaveRightToWorkCheck, RightToWorkCheck>
{
    private readonly JpmsContext context;

    public SaveRightToWorkCheckHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<RightToWorkCheck> HandleAsync(SaveRightToWorkCheck command, CancellationToken cancellationToken)
    {
        var check = await CheckToWriteAsync(command.RightToWorkCheckId, cancellationToken);
        RightToWorkCheckWriting.Apply(check, command.Details);
        check.EngagementEndedOn = command.EngagementEndedOn;
        check.RecordedByEmail = command.RecordedByEmail;
        check.RecordedAt = DateTimeOffset.UtcNow;
        await MarkTheFormHandledAsync(command.Details.FormSubmissionId, command.RecordedByEmail, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        var evidence = await context.FormUploads.AsNoTracking()
            .FirstOrDefaultAsync(row => row.FormUploadId == check.EvidenceUploadId, cancellationToken);
        return check.ToModel(evidence?.FileName ?? "");
    }

    private async Task<RightToWorkCheckEntity> CheckToWriteAsync(string? rightToWorkCheckId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rightToWorkCheckId))
        {
            var created = new RightToWorkCheckEntity { RightToWorkCheckId = FormIdentifierFactory.NextId() };
            context.RightToWorkChecks.Add(created);
            return created;
        }
        return await context.RightToWorkChecks.FirstOrDefaultAsync(row => row.RightToWorkCheckId == rightToWorkCheckId, cancellationToken)
            ?? throw new InvalidOperationException("That check no longer exists.");
    }

    private async Task MarkTheFormHandledAsync(string? formSubmissionId, string handledByEmail, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(formSubmissionId)) return;
        var form = await context.FormSubmissions.FirstOrDefaultAsync(row => row.FormSubmissionId == formSubmissionId, cancellationToken);
        if (form is null) return;
        form.Status = (int)FormSubmissionStatus.Handled;
        form.HandledByEmail = handledByEmail;
        form.HandledAt = DateTimeOffset.UtcNow;
    }
}

public sealed class SaveRightToWorkCheckAuthorisation
{
    public bool Allows(SignedInUser user, SaveRightToWorkCheck command) => FormRoleSets.RightToWorkReaders.IncludesAny(user.Roles);
}

public sealed class SaveRightToWorkCheckValidation
{
    public ValidationOutcome Check(SaveRightToWorkCheck command)
    {
        if (command.Details is null) return ValidationOutcome.Failed("The check's details are required.");
        var problems = RightToWorkRules.ProblemsWith(command.Details, FormClock.Today());
        return problems.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(problems);
    }
}
