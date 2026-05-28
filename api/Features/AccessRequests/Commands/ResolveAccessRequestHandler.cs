using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.AccessRequests;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.AccessRequests.Commands;

public sealed class ResolveAccessRequestHandler
    : ICommandHandler<ResolveAccessRequest, Acknowledgement>
{
    private readonly JpmsContext context;

    public ResolveAccessRequestHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(ResolveAccessRequest command, CancellationToken cancellationToken)
    {
        var entity = await context.AccessRequests
            .FirstOrDefaultAsync(request => request.Email == command.Email, cancellationToken);
        if (entity is not null)
        {
            context.AccessRequests.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
        return new Acknowledgement(command.Email);
    }
}
