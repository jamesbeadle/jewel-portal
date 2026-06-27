using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class ReturnRequestToTriageHandler : ICommandHandler<ReturnRequestToTriage, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly IMailboxActionScheduler mailbox;
    public ReturnRequestToTriageHandler(JpmsContext context, IMailboxActionScheduler mailbox) { this.context = context; this.mailbox = mailbox; }

    public async Task<Acknowledgement> HandleAsync(ReturnRequestToTriage command, CancellationToken cancellationToken)
    {
        // Send every email linked to this request back to the queue: re-open it for triage and clear
        // its link so it is no longer associated with the (about-to-be-deleted) request.
        var linkedEmails = await context.IntakeEmails
            .Where(e => e.LinkedRequestId == command.RequestId)
            .ToListAsync(cancellationToken);

        foreach (var email in linkedEmails)
        {
            email.Status = (int)IntakeStatus.NeedsTriage;
            email.LinkedRequestId = null;
        }

        // Drop the request's conversation history. The thread will be rebuilt when re-triaged, and
        // future replies must not match a deleted request (auto-link checks the request still exists).
        var messages = await context.RequestMessages
            .Where(m => m.RequestId == command.RequestId)
            .ToListAsync(cancellationToken);
        context.RequestMessages.RemoveRange(messages);

        // Delete the request if returning the emails has emptied it (nothing left to keep).
        var stillLinked = await context.IntakeEmails
            .AnyAsync(e => e.LinkedRequestId == command.RequestId, cancellationToken);
        if (!stillLinked)
        {
            var request = await context.Requests
                .FirstOrDefaultAsync(r => r.RequestId == command.RequestId, cancellationToken);
            if (request is not null) context.Requests.Remove(request);
        }

        await context.SaveChangesAsync(cancellationToken);

        // Now that the DB is the source of truth again, move each email back to the Inbox best-effort.
        foreach (var email in linkedEmails)
            await mailbox.ScheduleReturnToInboxAsync(email.IntakeId, cancellationToken);

        return new Acknowledgement(command.RequestId);
    }
}
