using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Changes;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Changes.Commands;

public sealed class RaiseChangeHandler : ICommandHandler<RaiseChange, ChangeRecord>
{
    private readonly JpmsContext context;
    public RaiseChangeHandler(JpmsContext context) { this.context = context; }

    public async Task<ChangeRecord> HandleAsync(RaiseChange command, CancellationToken cancellationToken)
    {
        var entity = new ChangeRecordEntity
        {
            ChangeRecordId = ChangesIdentifierFactory.Next(),
            ProjectId = command.ProjectId,
            Kind = (int)command.Kind,
            Reference = command.Reference,
            Title = command.Title,
            Description = command.Description,
            Status = (int)ChangeStatus.Open,
            Value = command.Value,
            RaisedByEmail = command.RaisedByEmail,
            RaisedAt = DateTimeOffset.UtcNow,
            RespondedAt = null,
            ResponseText = null,
            RespondedByEmail = null,
            ImpliesVariation = false
        };
        context.ChangeRecords.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
