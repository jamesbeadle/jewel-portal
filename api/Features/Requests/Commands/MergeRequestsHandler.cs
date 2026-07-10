using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

/// <summary>
/// Combines two General requests into one before either is promoted to an RFI — the fix for the
/// same query arriving twice through triage (e.g. an email thread and a raised request covering
/// the same defect). The survivor keeps its reference and title; everything else folds in:
///
///  1. The merged request's conversation history (RequestMessages) moves to the survivor.
///  2. Its itemised queries (RequestItems) append after the survivor's, keeping their order.
///  3. Its description is appended beneath the survivor's, labelled with its reference.
///  4. Its live-read emails are retagged to the survivor's workflow tag, so the correspondence
///     follows (same mechanism as a reference rename).
///  5. The merged request is closed and stamped MergedIntoRequestId/MergedAt — kept for the audit
///     trail but never counted as open again. A system message on the survivor records the merge.
/// </summary>
public sealed class MergeRequestsHandler : ICommandHandler<MergeRequests, Request>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    public MergeRequestsHandler(JpmsContext context, IMailboxGraphClient graph) { this.context = context; this.graph = graph; }

    public async Task<Request> HandleAsync(MergeRequests command, CancellationToken cancellationToken)
    {
        var survivor = await context.Requests
            .FirstOrDefaultAsync(r => r.RequestId == command.SurvivorRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"Request '{command.SurvivorRequestId}' not found.");
        var merged = await context.Requests
            .FirstOrDefaultAsync(r => r.RequestId == command.MergedRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"Request '{command.MergedRequestId}' not found.");

        if (survivor.ProjectId != merged.ProjectId)
            throw new InvalidOperationException("Requests can only be merged within the same project.");
        if ((RequestType)survivor.Kind != RequestType.General || (RequestType)merged.Kind != RequestType.General)
            throw new InvalidOperationException("Only General requests can be merged — once a request is promoted (RFI onward) it carries an official document and must stay separate.");
        if (survivor.MergedIntoRequestId is not null || merged.MergedIntoRequestId is not null)
            throw new InvalidOperationException("One of these requests has already been merged.");

        // 1. Conversation history moves across wholesale.
        var messages = await context.RequestMessages
            .Where(m => m.RequestId == merged.RequestId)
            .ToListAsync(cancellationToken);
        foreach (var message in messages) message.RequestId = survivor.RequestId;

        // 2. Itemised queries append after the survivor's, keeping their relative order.
        var nextPosition = (await context.RequestItems
            .Where(i => i.RequestId == survivor.RequestId)
            .Select(i => (int?)i.Position)
            .MaxAsync(cancellationToken)) ?? 0;
        var items = await context.RequestItems
            .Where(i => i.RequestId == merged.RequestId)
            .OrderBy(i => i.Position)
            .ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            item.RequestId = survivor.RequestId;
            item.Position = ++nextPosition;
        }

        // 3. The merged description folds in beneath the survivor's, labelled with where it came
        //    from (truncated to the column's limit if the combination overruns it).
        if (!string.IsNullOrWhiteSpace(merged.Description))
        {
            var combined = string.IsNullOrWhiteSpace(survivor.Description)
                ? merged.Description
                : $"{survivor.Description}\n\n— Merged from {merged.TagReference} ({merged.Title}) —\n{merged.Description}";
            survivor.Description = combined.Length <= 2048 ? combined : combined[..2048];
        }

        // 4. The live-read emails follow: retag everything carrying the merged request's workflow
        //    tag onto the survivor's (the tag is the only link — no copies are stored). Same
        //    add-before-remove mechanism as a reference rename, so nothing bounces back to triage.
        var mergedTag = TriageCategories.ForRequest(await RequestTags.StemAsync(context, merged, cancellationToken));
        var survivorTag = TriageCategories.ForRequest(await RequestTags.StemAsync(context, survivor, cancellationToken));
        await graph.RetagAsync(mergedTag, survivorTag, cancellationToken);

        // 5. Close the merged request and stamp the audit link, then note the merge on the
        //    survivor's conversation so the combined history explains itself.
        merged.Status = (int)RequestStatus.Closed;
        merged.ClosedAt = DateTimeOffset.UtcNow; // closed by the merge itself, so the close date is the merge date
        merged.MergedIntoRequestId = survivor.RequestId;
        merged.MergedAt = DateTimeOffset.UtcNow;

        context.RequestMessages.Add(new RequestMessageEntity
        {
            MessageId = RequestsIdentifierFactory.Next(),
            RequestId = survivor.RequestId,
            AuthorEmail = "",
            AuthorName = "JPMS",
            Body = $"{merged.TagReference} ({merged.Title}) was merged into this request — its conversation, queries and emails now live here.",
            Visibility = (int)MessageVisibility.Internal,
            PostedAt = DateTimeOffset.UtcNow
        });

        await context.SaveChangesAsync(cancellationToken);
        return survivor.ToModel();
    }
}
