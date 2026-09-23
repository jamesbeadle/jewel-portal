using System.Text.RegularExpressions;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Forms.Mail;
using Jewel.JPMS.Api.Features.Forms.Mapping;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Registers;

/// <summary>
/// Emailing a person that their right-to-work check was completed (the register's "Send
/// confirmation"): only a completed pass is confirmed, only to an address on the record, and the day
/// it went is kept on the check and in the audit trail.
/// </summary>
public sealed class SendRightToWorkConfirmationHandler : ICommandHandler<SendRightToWorkConfirmation, RightToWorkCheck>
{
    private const string CouldNotSend = "The email could not be sent just now.";
    private readonly JpmsContext context;
    private readonly IFormMailer mailer;
    private readonly AuditTrail audit;

    public SendRightToWorkConfirmationHandler(JpmsContext context, IFormMailer mailer, AuditTrail audit)
    {
        this.context = context;
        this.mailer = mailer;
        this.audit = audit;
    }

    public async Task<RightToWorkCheck> HandleAsync(SendRightToWorkConfirmation command, CancellationToken cancellationToken)
    {
        var check = await context.RightToWorkChecks.FirstOrDefaultAsync(row => row.RightToWorkCheckId == command.RightToWorkCheckId, cancellationToken)
            ?? throw new InvalidOperationException("That check no longer exists.");
        var details = check.DetailsOf();
        RightToWorkConfirmationRules.Check(details);
        await SendAsync(details, cancellationToken);
        check.ConfirmedOn = FormClock.Today();
        await context.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(AuditEventType.RightToWorkConfirmationSent,
            detail: $"Confirmation emailed of the right to work check of {details.CheckedOn:yyyy-MM-dd}",
            recordId: check.RightToWorkCheckId, actorEmail: command.SentByEmail, cancellationToken: cancellationToken);
        var evidence = await context.FormUploads.AsNoTracking().FirstOrDefaultAsync(row => row.FormUploadId == check.EvidenceUploadId, cancellationToken);
        return check.ToModel(evidence?.FileName ?? "");
    }

    private async Task SendAsync(RightToWorkCheckDetails details, CancellationToken cancellationToken)
    {
        try { await mailer.SendAsync(FormRightToWorkEmails.CheckCompleted(details), cancellationToken); }
        catch (Exception failure) when (failure is not OperationCanceledException) { throw new InvalidOperationException(CouldNotSend, failure); }
    }
}

/// <summary>What a check must be before its confirmation goes: a completed pass, with an address to send it to.</summary>
internal static class RightToWorkConfirmationRules
{
    private static readonly Regex EmailAddress = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    public static void Check(RightToWorkCheckDetails details)
    {
        if (!EmailAddress.IsMatch(details.Email)) throw new InvalidOperationException("No email address on the record. Open the row and add one.");
        if (!RightToWorkRules.IsCleared(details)) throw new InvalidOperationException("Only a completed pass can be confirmed. Finish the check first.");
    }
}

public sealed class SendRightToWorkConfirmationAuthorisation
{
    public bool Allows(SignedInUser user, SendRightToWorkConfirmation command) => FormRoleSets.RightToWorkReaders.IncludesAny(user.Roles);
}

public sealed class SendRightToWorkConfirmationValidation
{
    public ValidationOutcome Check(SendRightToWorkConfirmation command) =>
        string.IsNullOrWhiteSpace(command.RightToWorkCheckId) ? ValidationOutcome.Failed("Which check?") : ValidationOutcome.Passed;
}
