using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Subcontractors;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class RemoveCompanyContactHandler : ICommandHandler<RemoveCompanyContact, Acknowledgement>
{
    private readonly JpmsContext context;

    public RemoveCompanyContactHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(RemoveCompanyContact command, CancellationToken cancellationToken)
    {
        var entity = await context.CompanyContacts.FirstOrDefaultAsync(
            contact => contact.CompanyContactId == command.CompanyContactId
                && contact.SubcontractorId == command.SubcontractorId,
            cancellationToken);
        if (entity is null)
            throw new InvalidOperationException("That contact was not found on this record.");

        context.CompanyContacts.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.CompanyContactId);
    }
}
