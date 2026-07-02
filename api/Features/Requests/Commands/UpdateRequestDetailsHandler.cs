using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class UpdateRequestDetailsHandler : ICommandHandler<UpdateRequestDetails, Request>
{
    private readonly JpmsContext context;
    private readonly IMailboxGraphClient graph;
    private readonly ILogger<UpdateRequestDetailsHandler> logger;
    public UpdateRequestDetailsHandler(JpmsContext context, IMailboxGraphClient graph, ILogger<UpdateRequestDetailsHandler> logger)
    {
        this.context = context;
        this.graph = graph;
        this.logger = logger;
    }

    public async Task<Request> HandleAsync(UpdateRequestDetails command, CancellationToken cancellationToken)
    {
        var entity = await context.Requests.FindAsync(new object[] { command.RequestId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Request {command.RequestId} not found.");

        // Allow the reference to be edited manually, but never onto a number another request on this
        // project already holds — excluding this request so an unchanged reference always passes.
        await RequestReferenceGuard.EnsureUniqueAsync(context, entity.ProjectId, command.Reference, entity.RequestId, cancellationToken);

        // The mailbox tag is derived from the (project-qualified) reference, so capture the old tag
        // before we change it. The project qualifier is the same on both sides of the rename.
        var projectRef = await RequestTags.ProjectRefAsync(context, entity.ProjectId, cancellationToken);
        var previousTag = TriageCategories.ForRecord(RequestTags.Stem(projectRef, entity.ProjectId, entity.TagReference));

        entity.Reference = command.Reference;
        entity.Title = command.Title;
        entity.Description = command.Description;
        entity.Status = (int)command.Status;
        entity.Value = command.Value;
        entity.ResponseText = command.ResponseText;
        entity.RespondedByEmail = command.RespondedByEmail;
        entity.ImpliesVariation = command.ImpliesVariation;
        entity.RaisedTo = command.RaisedTo;
        entity.DrawingRef = command.DrawingRef;
        entity.ResponseDue = command.ResponseDue;
        entity.RelatedDrawingSpec = command.RelatedDrawingSpec;
        entity.InternalNotes = command.InternalNotes;
        entity.ClientNotes = command.ClientNotes;
        if (command.RaisedAt is { } issued) entity.RaisedAt = issued;
        if (entity.RespondedAt is null && !string.IsNullOrWhiteSpace(command.ResponseText)) entity.RespondedAt = DateTimeOffset.UtcNow;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (RequestReferenceConflict.IsReferenceClash(ex))
        {
            // The guard above is check-then-act and can race; the unique index is the backstop.
            throw RequestReferenceConflict.AsFriendlyError(command.Reference);
        }

        // If the reference changed, its mailbox tag changed with it. Move every email carrying the old
        // JPMS/<ref> tag onto the new one so the record keeps its linked correspondence — including
        // replies further down the tagged threads. Best-effort: the reference is already saved, and a
        // transient Graph failure shouldn't fail the edit (a later thread-sync can reconcile).
        var newTag = TriageCategories.ForRecord(RequestTags.Stem(projectRef, entity.ProjectId, entity.TagReference));
        if (!string.Equals(previousTag, newTag, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var moved = await graph.RetagAsync(previousTag, newTag, cancellationToken);
                logger.LogInformation("Retagged {Count} email(s) from {OldTag} to {NewTag} after reference change.",
                    moved, previousTag, newTag);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Retag from {OldTag} to {NewTag} failed; emails may still carry the old tag.",
                    previousTag, newTag);
            }
        }

        return entity.ToModel();
    }
}
