using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.AccessRequests;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.AccessRequests.Commands;

public sealed class SubmitAccessRequestHandler
    : ICommandHandler<SubmitAccessRequest, AccessRequest>
{
    private readonly JpmsContext context;

    public SubmitAccessRequestHandler(JpmsContext context) { this.context = context; }

    public async Task<AccessRequest> HandleAsync(SubmitAccessRequest command, CancellationToken cancellationToken)
    {
        var entity = await context.AccessRequests
            .FirstOrDefaultAsync(request => request.Email == command.Email, cancellationToken);
        if (entity is null)
        {
            entity = new AccessRequestEntity { Email = command.Email };
            context.AccessRequests.Add(entity);
        }
        entity.DisplayName = command.DisplayName;
        entity.RequestedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
