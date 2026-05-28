using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Closeout;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Closeout.Commands;

public sealed class ReleaseRetentionHandler : ICommandHandler<ReleaseRetention, RetentionRelease>
{
    private readonly JpmsContext context;
    public ReleaseRetentionHandler(JpmsContext context) { this.context = context; }

    public async Task<RetentionRelease> HandleAsync(ReleaseRetention command, CancellationToken cancellationToken)
    {
        var entity = new RetentionReleaseEntity
        {
            RetentionReleaseId = CloseoutIdentifierFactory.NextRetentionReleaseId(),
            ProjectId = command.ProjectId,
            Amount = command.Amount,
            ReleasedAt = DateTimeOffset.UtcNow,
            IsPublishedDownstream = command.IsPublishedDownstream
        };
        context.RetentionReleases.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
