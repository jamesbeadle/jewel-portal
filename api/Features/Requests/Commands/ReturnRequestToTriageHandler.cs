using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Undo a triage decision: send the request's emails back to triage. In the live-read model the
/// request's emails live in its mailbox folder, so their request tags are cleared and they
/// re-enter the Inbox queue. The request itself and its conversation history are kept — triage
/// only assigns email context to records, so undoing a triage decision must never destroy the
/// records that were created (an RFI is an official document and survives regardless). The one
/// exception is a stranded request with no live project, whose disposal is the entire point of
/// the unassigned-requests recovery flow.
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

        // Clear the request's tags from its emails so they re-enter the triage queue (best-effort).
        // The request row and its conversation history deliberately stay untouched.
        await graph.ClearRequestTagsAsync(TriageCategories.ForRequest(await RequestTags.StemAsync(context, request, cancellationToken)), cancellationToken);

        // One exception: a stranded request (blank project id, or one that no longer matches any
        // project) is a broken row that can never appear in a register. The unassigned-requests
        // recovery panel returns it to triage precisely to dispose of it, so only then is the row
        // (and its conversation) removed. Requests on live projects always survive.
        var isStranded = string.IsNullOrEmpty(request.ProjectId)
            || !await context.Projects.AnyAsync(p => p.ProjectId == request.ProjectId, cancellationToken);
        if (isStranded)
        {
            var messages = await context.RequestMessages
                .Where(m => m.RequestId == command.RequestId)
                .ToListAsync(cancellationToken);
            context.RequestMessages.RemoveRange(messages);
            context.Requests.Remove(request);
            await context.SaveChangesAsync(cancellationToken);
        }

        return new Acknowledgement(command.RequestId);
    }
}
