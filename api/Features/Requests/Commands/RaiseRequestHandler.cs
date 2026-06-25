using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class RaiseRequestHandler : ICommandHandler<RaiseRequest, Request>
{
    private readonly JpmsContext context;
    public RaiseRequestHandler(JpmsContext context) { this.context = context; }

    public async Task<Request> HandleAsync(RaiseRequest command, CancellationToken cancellationToken)
    {
        var entity = new RequestEntity
        {
            RequestId = RequestsIdentifierFactory.Next(),
            ProjectId = command.ProjectId,
            Kind = (int)command.Kind,
            Reference = command.Reference,
            Title = command.Title,
            Description = command.Description,
            Status = (int)RequestStatus.Open,
            Value = command.Value,
            RaisedByEmail = command.RaisedByEmail,
            RaisedAt = DateTimeOffset.UtcNow,
            RespondedAt = null,
            ResponseText = null,
            RespondedByEmail = null,
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
