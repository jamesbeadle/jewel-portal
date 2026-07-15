using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Retention;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Retention.Commands;

// Freezes the released amount on the record and stamps the confirmation time. Validation
// guarantees the milestone hasn't already been confirmed and the record exists.
public sealed class ConfirmRetentionReleaseHandler : ICommandHandler<ConfirmRetentionRelease, ProjectRetention>
{
    private readonly JpmsContext context;

    public ConfirmRetentionReleaseHandler(JpmsContext context) { this.context = context; }

    public async Task<ProjectRetention> HandleAsync(ConfirmRetentionRelease command, CancellationToken cancellationToken)
    {
        var entity = await context.ProjectRetentions.FirstAsync(
            retention => retention.ProjectId == command.ProjectId, cancellationToken);

        if (command.Milestone == RetentionMilestone.Completion)
        {
            if (entity.CompletionReleaseConfirmedAt is not null)
                throw new InvalidOperationException("The completion release has already been confirmed.");
            entity.CompletionReleaseAmount = command.Amount;
            entity.CompletionReleaseConfirmedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            if (entity.FinalReleaseConfirmedAt is not null)
                throw new InvalidOperationException("The final release has already been confirmed.");
            entity.FinalReleaseAmount = command.Amount;
            entity.FinalReleaseConfirmedAt = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
