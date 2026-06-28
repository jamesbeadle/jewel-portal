using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Projects;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Projects.Contacts;

public sealed class RemoveProjectContactHandler : ICommandHandler<RemoveProjectContact, Acknowledgement>
{
    private readonly JpmsContext context;
    public RemoveProjectContactHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(RemoveProjectContact command, CancellationToken cancellationToken)
    {
        var entity = await context.ProjectContacts
            .FirstOrDefaultAsync(c => c.ContactId == command.ContactId && c.ProjectId == command.ProjectId, cancellationToken);
        if (entity is not null)
        {
            context.ProjectContacts.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
        return new Acknowledgement(command.ContactId);
    }
}
