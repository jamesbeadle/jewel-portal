using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class RestoreIntakeEmailHandler : ICommandHandler<RestoreIntakeEmail, IntakeEmail>
{
    private readonly JpmsContext context;
    private readonly IMailboxActionScheduler mailbox;
    public RestoreIntakeEmailHandler(JpmsContext context, IMailboxActionScheduler mailbox) { this.context = context; this.mailbox = mailbox; }

    public async Task<IntakeEmail> HandleAsync(RestoreIntakeEmail command, CancellationToken cancellationToken)
    {
        var entity = await context.IntakeEmails.FirstOrDefaultAsync(e => e.IntakeId == command.IntakeId, cancellationToken)
            ?? throw new InvalidOperationException($"Intake email {command.IntakeId} not found.");

        // Only a discarded email can be restored. Anything else (still in triage, linked to a
        // request, or removed from the mailbox) is left untouched so a stray call can never pull a
        // legitimately-triaged email back into the queue. Idempotent: a no-op returns as-is.
        if (entity.Status != (int)IntakeStatus.Discarded)
            return entity.ToModel();

        entity.Status = (int)IntakeStatus.NeedsTriage;
        await context.SaveChangesAsync(cancellationToken);

        // Move the mailbox copy out of "Not relevant" back into the Inbox so the queue mirrors the
        // mailbox again. Best-effort (queued) — the DB change above is the guarantee.
        await mailbox.ScheduleReturnToInboxAsync(entity.IntakeId, cancellationToken);
        return entity.ToModel();
    }
}
