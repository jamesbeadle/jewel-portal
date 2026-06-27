using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests;

// When an email is triaged onto a request, any *other* emails still awaiting triage that belong
// to the same mail thread almost certainly belong to that request too. Rather than make the
// triager process them one by one, we sweep every sibling sharing the originating email's
// ConversationId into the request in the same action: each becomes an inbound message, is marked
// Linked, and is queued to move into the request's mailbox folder. Threads are matched on
// ConversationId only (the reliable Graph thread key); emails with no ConversationId are skipped.
internal static class ThreadGather
{
    public static async Task SweepSiblingsAsync(
        JpmsContext context,
        IMailboxActionScheduler mailbox,
        IntakeEmailEntity origin,
        string requestId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(origin.ConversationId))
            return;

        var siblings = await context.IntakeEmails
            .Where(e => e.ConversationId == origin.ConversationId
                        && e.IntakeId != origin.IntakeId
                        && e.Status == (int)IntakeStatus.NeedsTriage)
            .ToListAsync(cancellationToken);

        if (siblings.Count == 0)
            return;

        foreach (var sibling in siblings)
        {
            context.RequestMessages.Add(IntakeConversation.AsInboundMessage(sibling, requestId));
            sibling.Status = (int)IntakeStatus.Linked;
            sibling.LinkedRequestId = requestId;
        }

        await context.SaveChangesAsync(cancellationToken);

        foreach (var sibling in siblings)
            await mailbox.ScheduleOutcomeMoveAsync(sibling.IntakeId, IntakeStatus.Linked, cancellationToken);
    }
}
