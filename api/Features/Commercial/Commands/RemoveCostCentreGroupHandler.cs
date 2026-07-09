using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Cqrs;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class RemoveCostCentreGroupHandler : ICommandHandler<RemoveCostCentreGroup, Acknowledgement>
{
    private readonly JpmsContext context;

    public RemoveCostCentreGroupHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(RemoveCostCentreGroup command, CancellationToken cancellationToken)
    {
        var group = await context.CostCentreGroups.FirstOrDefaultAsync(
            candidate => candidate.CostCentreGroupId == command.CostCentreGroupId
                         && candidate.ProjectId == command.ProjectId, cancellationToken);
        if (group is null) return new Acknowledgement(command.CostCentreGroupId); // already gone — idempotent

        var members = await context.CostCentreGroupMembers
            .Where(member => member.CostCentreGroupId == group.CostCentreGroupId)
            .ToListAsync(cancellationToken);

        context.CostCentreGroupMembers.RemoveRange(members);
        context.CostCentreGroups.Remove(group);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(group.CostCentreGroupId);
    }
}
