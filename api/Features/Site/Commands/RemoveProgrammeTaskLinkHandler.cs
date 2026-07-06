using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Site;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Site.Commands;

public sealed class RemoveProgrammeTaskLinkHandler : ICommandHandler<RemoveProgrammeTaskLink, Acknowledgement>
{
    private readonly JpmsContext context;
    public RemoveProgrammeTaskLinkHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(RemoveProgrammeTaskLink command, CancellationToken cancellationToken)
    {
        var entity = await context.ProgrammeTaskLinks
            .FirstOrDefaultAsync(l => l.ProgrammeTaskLinkId == command.ProgrammeTaskLinkId, cancellationToken);
        if (entity is not null)
        {
            context.ProgrammeTaskLinks.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
        return new Acknowledgement(command.ProgrammeTaskLinkId);
    }
}
