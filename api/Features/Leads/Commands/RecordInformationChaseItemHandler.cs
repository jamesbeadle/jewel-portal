using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class RecordInformationChaseItemHandler
    : ICommandHandler<RecordInformationChaseItem, InfoChaseItem>
{
    private readonly JpmsContext context;

    public RecordInformationChaseItemHandler(JpmsContext context) { this.context = context; }

    public async Task<InfoChaseItem> HandleAsync(
        RecordInformationChaseItem command, CancellationToken cancellationToken)
    {
        var entity = new InfoChaseItemEntity
        {
            InfoChaseItemId = LeadIdentifierFactory.NextInfoChaseItemId(),
            LeadId = command.LeadId,
            Kind = command.Kind,
            Description = command.Description,
            IsReceived = command.IsReceived,
            RequestedAt = DateTimeOffset.UtcNow
        };
        context.InfoChaseItems.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
