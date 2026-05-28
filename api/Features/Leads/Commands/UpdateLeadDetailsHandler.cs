using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Leads;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Leads.Commands;

public sealed class UpdateLeadDetailsHandler
    : ICommandHandler<UpdateLeadDetails, Lead>
{
    private readonly JpmsContext context;

    public UpdateLeadDetailsHandler(JpmsContext context) { this.context = context; }

    public async Task<Lead> HandleAsync(UpdateLeadDetails command, CancellationToken cancellationToken)
    {
        var entity = await context.Leads.FindAsync(new object[] { command.LeadId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Lead {command.LeadId} not found.");

        entity.Reference = command.Reference;
        entity.ContactName = command.ContactName;
        entity.ContactEmail = command.ContactEmail;
        entity.ContactPhone = command.ContactPhone;
        entity.CompanyName = command.CompanyName;
        entity.SiteAddress = command.SiteAddress;
        entity.EstimatedValue = command.EstimatedValue;
        entity.Source = (int)command.Source;
        entity.Stage = (int)command.Stage;
        entity.OwnerEmail = command.OwnerEmail;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
