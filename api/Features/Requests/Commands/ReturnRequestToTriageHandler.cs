using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Dismantle a request and send its emails back to triage. In the live-read model the request's
/// emails live in its mailbox folder, so they are moved back to the Inbox (where they re-enter the
/// queue), and the request plus its conversation history are deleted — a later reply must not match
/// a request that no longer exists.
/// </summary>
public sealed class ReturnRequestToTriageHandler : ICommandHandler<ReturnRequestToTriage, Acknowledgement>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    public ReturnRequestToTriageHandler(JpmsContext context, IMailboxGraphClient graph) { this.context = context; this.graph = graph; }

    public async Task<Acknowledgement> HandleAsync(ReturnRequestToTriage command, CancellationToken cancellationToken)
    {
        var request = await context.Requests
            .FirstOrDefaultAsync(r => r.RequestId == command.RequestId, cancellationToken);
        if (request is null)
            return new Acknowledgement(command.RequestId);

        // Move the request's emails back to the Inbox so they re-enter triage (best-effort).
        if (!string.IsNullOrEmpty(request.MailboxFolderId))
            await graph.ReturnFolderMessagesToInboxAsync(request.MailboxFolderId, cancellationToken);

        // Drop the request's conversation history and the request itself.
        var messages = await context.RequestMessages
            .Where(m => m.RequestId == command.RequestId)
            .ToListAsync(cancellationToken);
        context.RequestMessages.RemoveRange(messages);
        context.Requests.Remove(request);
        await context.SaveChangesAsync(cancellationToken);

        return new Acknowledgement(command.RequestId);
    }
}
