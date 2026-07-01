using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Subcontractors.Commands;

public sealed class AddSubcontractorToDirectoryHandler
    : ICommandHandler<AddSubcontractorToDirectory, Subcontractor>
{
    private readonly JpmsContext context;

    public AddSubcontractorToDirectoryHandler(JpmsContext context) { this.context = context; }

    public async Task<Subcontractor> HandleAsync(AddSubcontractorToDirectory command, CancellationToken cancellationToken)
    {
        var entity = new SubcontractorEntity
        {
            SubcontractorId = SubcontractorIdentifierFactory.NextSubcontractorId(),
            CompanyName = command.CompanyName,
            PrimaryTrade = command.PrimaryTrade,
            ContactName = command.ContactName,
            ContactEmail = command.ContactEmail,
            ContactPhone = command.ContactPhone,
            CisStatus = command.CisStatus,
            OnboardedAt = DateTimeOffset.UtcNow,
            Category = (int)command.Category,
            MobileNumber = command.MobileNumber,
            Town = command.Town,
            County = command.County,
            Website = command.Website
        };
        context.Subcontractors.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
