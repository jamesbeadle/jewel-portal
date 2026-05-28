using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class CaptureLeadHandler
    : ICommandHandler<CaptureLead, Lead>
{
    private readonly JpmsContext context;

    public CaptureLeadHandler(JpmsContext context) { this.context = context; }

    public async Task<Lead> HandleAsync(CaptureLead command, CancellationToken cancellationToken)
    {
        var entity = new LeadEntity
        {
            LeadId = LeadIdentifierFactory.NextLeadId(),
            Reference = command.Reference,
            ContactName = command.ContactName,
            ContactEmail = command.ContactEmail,
            ContactPhone = command.ContactPhone,
            CompanyName = command.CompanyName,
            SiteAddress = command.SiteAddress,
            EstimatedValue = command.EstimatedValue,
            Source = (int)command.Source,
            Stage = (int)LeadStage.NewLead,
            OwnerEmail = command.OwnerEmail,
            CapturedAt = DateTimeOffset.UtcNow
        };
        context.Leads.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
