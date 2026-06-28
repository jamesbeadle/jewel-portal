using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Requests.Commands;

public sealed class UpdateRequestDetailsHandler : ICommandHandler<UpdateRequestDetails, Request>
{
    private readonly JpmsContext context;
    public UpdateRequestDetailsHandler(JpmsContext context) { this.context = context; }

    public async Task<Request> HandleAsync(UpdateRequestDetails command, CancellationToken cancellationToken)
    {
        var entity = await context.Requests.FindAsync(new object[] { command.RequestId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Request {command.RequestId} not found.");

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

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
