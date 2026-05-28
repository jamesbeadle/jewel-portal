using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class UpdateSubcontractorHandler
    : ICommandHandler<UpdateSubcontractor, Subcontractor>
{
    private readonly JpmsContext context;

    public UpdateSubcontractorHandler(JpmsContext context) { this.context = context; }

    public async Task<Subcontractor> HandleAsync(UpdateSubcontractor command, CancellationToken cancellationToken)
    {
        var entity = await context.Subcontractors.FindAsync(new object[] { command.SubcontractorId }, cancellationToken);
        if (entity is null) throw new InvalidOperationException($"Subcontractor {command.SubcontractorId} not found.");

        entity.CompanyName = command.CompanyName;
        entity.PrimaryTrade = command.PrimaryTrade;
        entity.ContactName = command.ContactName;
        entity.ContactEmail = command.ContactEmail;
        entity.ContactPhone = command.ContactPhone;
        entity.CisStatus = command.CisStatus;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
