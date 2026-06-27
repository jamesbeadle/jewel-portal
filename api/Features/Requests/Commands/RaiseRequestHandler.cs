using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class RaiseRequestHandler : ICommandHandler<RaiseRequest, Request>
{
    private readonly JpmsContext context;
    public RaiseRequestHandler(JpmsContext context) { this.context = context; }

    public async Task<Request> HandleAsync(RaiseRequest command, CancellationToken cancellationToken)
    {
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
        return entity.ToModel();
    }
}
