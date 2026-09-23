using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Mapping;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Office.Submissions;

/// <summary>New, in progress or handled (api/registers.js op:'formstatus'); handling stamps who and when.</summary>
public sealed class SetFormSubmissionStatusHandler : ICommandHandler<SetFormSubmissionStatus, FormSubmission>
{
    private readonly JpmsContext context;

    public SetFormSubmissionStatusHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<FormSubmission> HandleAsync(SetFormSubmissionStatus command, CancellationToken cancellationToken)
    {
        var submission = await context.FormSubmissions.FirstOrDefaultAsync(row => row.FormSubmissionId == command.FormSubmissionId, cancellationToken)
            ?? throw new InvalidOperationException("That form no longer exists.");
        if (submission.DestroyedAt is not null) throw new InvalidOperationException("That form has been destroyed under the retention rules.");
        submission.Status = (int)command.Status;
        var isHandled = command.Status == FormSubmissionStatus.Handled;
        if (isHandled) MarkHandled(submission, command.HandledByEmail);
        await context.SaveChangesAsync(cancellationToken);
        return submission.ToModel();
    }

    private static void MarkHandled(FormSubmissionEntity submission, string handledByEmail)
    {
        submission.HandledByEmail = handledByEmail;
        submission.HandledAt = DateTimeOffset.UtcNow;
    }
}

public sealed class SetFormSubmissionStatusAuthorisation
{
    public bool Allows(SignedInUser user, SetFormSubmissionStatus command) => FormRoleSets.Office.IncludesAny(user.Roles);
}

public sealed class SetFormSubmissionStatusValidation
{
    public ValidationOutcome Check(SetFormSubmissionStatus command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.FormSubmissionId)) errors.Add("Which form?");
        var isAnOfficeStatus = Enum.IsDefined(command.Status) && command.Status != FormSubmissionStatus.Destroyed;
        if (!isAnOfficeStatus) errors.Add("A form is New, In progress or Handled.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
