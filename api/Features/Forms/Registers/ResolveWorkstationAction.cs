using Jewel.JPMS.Api.Features.Forms.Mapping;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Registers;

/// <summary>
/// Closing one NO from a workstation assessment: fixed, or accepted with the reason it stands. An
/// assessment is not finished until every one of its noes is — so the last one closed hands the
/// assessment itself in as handled.
/// </summary>
public sealed class ResolveWorkstationActionHandler : ICommandHandler<ResolveWorkstationAction, WorkstationAction>
{
    private readonly JpmsContext context;

    public ResolveWorkstationActionHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<WorkstationAction> HandleAsync(ResolveWorkstationAction command, CancellationToken cancellationToken)
    {
        var action = await context.WorkstationActions.FirstOrDefaultAsync(row => row.WorkstationActionId == command.WorkstationActionId, cancellationToken)
            ?? throw new InvalidOperationException("That action no longer exists.");
        var note = command.Note?.Trim() ?? "";
        action.State = (int)command.State;
        action.Note = note;
        action.ResolvedByEmail = command.ResolvedByEmail;
        action.ResolvedAt = DateTimeOffset.UtcNow;
        await FinishAssessmentAsync(action.FormSubmissionId, action.WorkstationActionId, command.ResolvedByEmail, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return action.ToModel();
    }

    private async Task FinishAssessmentAsync(
        string formSubmissionId, string resolvedActionId, string resolvedByEmail, CancellationToken cancellationToken)
    {
        var stillOpen = await context.WorkstationActions.AnyAsync(
            row => row.FormSubmissionId == formSubmissionId && row.WorkstationActionId != resolvedActionId
                && row.State == (int)WorkstationActionState.Open, cancellationToken);
        if (stillOpen) return;
        var assessment = await context.FormSubmissions.FirstOrDefaultAsync(row => row.FormSubmissionId == formSubmissionId, cancellationToken);
        if (assessment is null) return;
        assessment.Status = (int)FormSubmissionStatus.Handled;
        assessment.HandledByEmail = resolvedByEmail;
        assessment.HandledAt = DateTimeOffset.UtcNow;
    }
}

public sealed class ResolveWorkstationActionAuthorisation
{
    public bool Allows(SignedInUser user, ResolveWorkstationAction command) => FormRoleSets.Office.IncludesAny(user.Roles);
}

public sealed class ResolveWorkstationActionValidation
{
    public ValidationOutcome Check(ResolveWorkstationAction command)
    {
        var errors = new List<string>();
        var isFinished = command.State is WorkstationActionState.Fixed or WorkstationActionState.Accepted;
        var isAcceptedWithoutAReason = command.State == WorkstationActionState.Accepted && string.IsNullOrWhiteSpace(command.Note);
        if (string.IsNullOrWhiteSpace(command.WorkstationActionId)) errors.Add("Which action?");
        if (!isFinished) errors.Add("An action is closed as fixed or as accepted.");
        if (isAcceptedWithoutAReason) errors.Add("Say why it is accepted rather than fixed.");
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}
