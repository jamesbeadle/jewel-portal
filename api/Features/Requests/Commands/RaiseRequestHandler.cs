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
    private readonly JpmsContext context;
    private readonly IMailboxActionScheduler mailbox;
    public RaiseRequestHandler(JpmsContext context, IMailboxActionScheduler mailbox) { this.context = context; this.mailbox = mailbox; }

    public async Task<Request> HandleAsync(RaiseRequest command, CancellationToken cancellationToken)
    {
        // The reference may have been typed manually, so guard against re-using a number already
        // on this project before we insert.
        await RequestReferenceGuard.EnsureUniqueAsync(context, command.ProjectId, command.Reference, excludeRequestId: null, cancellationToken);

        var nextNumber = (await context.Requests.MaxAsync(r => (int?)r.Number, cancellationToken) ?? 0) + 1;

        var entity = new RequestEntity
        {
            RequestId = RequestsIdentifierFactory.Next(),
            Number = nextNumber,
            ProjectId = command.ProjectId,
            Kind = (int)command.Kind,
            Reference = command.Reference,
            Title = command.Title,
            Description = command.Description,
            Status = (int)(command.Status ?? RequestStatus.Open),
            Value = command.Value,
            RaisedByEmail = command.RaisedByEmail,
            RaisedAt = command.RaisedAt ?? DateTimeOffset.UtcNow,
            RespondedAt = command.RespondedAt,
            ResponseText = command.ResponseText,
            RespondedByEmail = command.RespondedByEmail,
            ImpliesVariation = false,
            RaisedTo = command.RaisedTo,
            DrawingRef = command.DrawingRef,
            ResponseDue = command.ResponseDue,
            RelatedDrawingSpec = null,
            InternalNotes = command.InternalNotes,
            ClientNotes = command.ClientNotes
        };
        context.Requests.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        // Issue the document to the project's contacts when a fresh (Open) request is raised. Requests
        // imported in an already-resolved state are skipped, and the send is a no-op when the mailbox
        // feature is unconfigured, so this is safe to call unconditionally on the creation path.
        if (entity.Status == (int)RequestStatus.Open)
            await mailbox.ScheduleRequestDocumentSendAsync(entity.RequestId, recipientOverride: null, cancellationToken);

        return entity.ToModel();
    }
}
