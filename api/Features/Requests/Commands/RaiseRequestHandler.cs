using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Actions;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class RaiseRequestHandler : ICommandHandler<RaiseRequest, Request>
{
    // Auto-minted references are retried on a unique-index clash (a concurrent create took the same
    // number); manual ones are not — the user picked that number, so they get the clash error instead.
    private const int MaxMintAttempts = 3;

    private readonly JpmsContext context;
    private readonly IMailboxActionScheduler mailbox;
    public RaiseRequestHandler(JpmsContext context, IMailboxActionScheduler mailbox) { this.context = context; this.mailbox = mailbox; }

    public async Task<Request> HandleAsync(RaiseRequest command, CancellationToken cancellationToken)
    {
        // The reference may have been typed manually, so guard against re-using a number already
        // on this project before we insert. (Fast path — the unique index is the real backstop.)
        await RequestReferenceGuard.EnsureUniqueAsync(context, command.ProjectId, command.Reference, excludeRequestId: null, cancellationToken);

        var manualReference = !string.IsNullOrWhiteSpace(command.Reference);

        RequestEntity entity;
        for (var attempt = 1; ; attempt++)
        {
            var nextNumber = (await context.Requests.MaxAsync(r => (int?)r.Number, cancellationToken) ?? 0) + 1;

            // A blank reference is minted server-side by kind: a General request is a container
            // numbered REQ-#### to match its display number (global sequence — the number doubles as
            // its mailbox folder name in the shared projects@ mailbox); any other kind continues the
            // project's own sequence (e.g. "RFI-048" -> "RFI-049"). A typed reference (e.g. a
            // back-filled legacy RFI) is honoured as given.
            var reference = manualReference
                ? command.Reference.Trim()
                : await MintReferenceAsync(command.ProjectId, command.Kind, nextNumber, cancellationToken);

            entity = new RequestEntity
            {
                RequestId = RequestsIdentifierFactory.Next(),
                Number = nextNumber,
                ProjectId = command.ProjectId,
                Kind = (int)command.Kind,
                Reference = reference,
                Title = command.Title,
                Description = command.Description,
                Status = (int)(command.Status ?? RequestStatus.Open),
                Value = command.Value,
                RaisedByEmail = command.RaisedByEmail,
                RaisedAt = command.RaisedAt ?? DateTimeOffset.UtcNow,
                RespondedAt = command.RespondedAt,
                ResponseText = command.ResponseText,
                RespondedByEmail = command.RespondedByEmail,
                // A backfilled record created already-Closed takes its response date as the close
                // date (the best evidence of when it actually closed), else the backfill moment.
                ClosedAt = (command.Status ?? RequestStatus.Open) == RequestStatus.Closed
                    ? command.RespondedAt ?? DateTimeOffset.UtcNow
                    : null,
                ImpliesVariation = false,
                RaisedTo = command.RaisedTo,
                DrawingRef = command.DrawingRef,
                ResponseDue = command.ResponseDue,
                RelatedDrawingSpec = null,
                InternalNotes = command.InternalNotes,
                ClientNotes = command.ClientNotes,
                // EOT -> NoD provenance only makes sense on an EOT; ignore it for every other kind.
                RelatedNodRequestId = command.Kind == RequestType.ExtensionOfTime ? command.RelatedNodRequestId : null
            };
            context.Requests.Add(entity);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                break;
            }
            catch (DbUpdateException ex) when (RequestReferenceConflict.IsReferenceClash(ex))
            {
                // Detach the rejected insert so the retry starts clean.
                context.Entry(entity).State = EntityState.Detached;
                if (manualReference || attempt >= MaxMintAttempts)
                    throw RequestReferenceConflict.AsFriendlyError(reference);
            }
        }

        // Draft the document email only for the emailable kinds (RFI / NOD / EOT — see
        // RequestTypeExtensions.IsEmailable). A General request (or RFA/RFC/RFQ/RFP) is never
        // emailed. Requests imported in an already-resolved state are skipped, and the draft is a
        // no-op when the mailbox feature is unconfigured.
        if (entity.Status == (int)RequestStatus.Open && command.Kind.IsEmailable())
            await mailbox.ScheduleRequestDocumentSendAsync(entity.RequestId, recipientOverride: null, cancellationToken);

        return entity.ToModel();
    }

    private async Task<string> MintReferenceAsync(string projectId, RequestType kind, int nextNumber, CancellationToken cancellationToken)
    {
        if (kind == RequestType.General) return $"REQ-{nextNumber:0000}";

        var projectReferences = await context.Requests
            .Where(r => r.ProjectId == projectId)
            .Select(r => r.Reference)
            .ToListAsync(cancellationToken);
        return RequestReference.SuggestNext(kind, projectReferences);
    }
}
