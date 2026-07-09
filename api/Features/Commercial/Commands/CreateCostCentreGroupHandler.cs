using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

public sealed class CreateCostCentreGroupHandler : ICommandHandler<CreateCostCentreGroup, CostCentreGroup>
{
    private readonly JpmsContext context;

    public CreateCostCentreGroupHandler(JpmsContext context) { this.context = context; }

    public async Task<CostCentreGroup> HandleAsync(CreateCostCentreGroup command, CancellationToken cancellationToken)
    {
        var codes = command.CostCodes
            .Select(costCode => costCode.Trim())
            .Where(costCode => costCode.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // A centre can only sit in one roll-up per project — the unique index is the real
        // guarantee, but check up-front so the user gets a friendly rejection, not a 500.
        var alreadyGrouped = await context.CostCentreGroupMembers
            .Where(member => member.ProjectId == command.ProjectId && codes.Contains(member.CostCode))
            .Select(member => member.CostCode)
            .ToListAsync(cancellationToken);
        if (alreadyGrouped.Count > 0)
            throw new InvalidOperationException(
                $"Already in another group: {string.Join(", ", alreadyGrouped)}. Ungroup first, then roll up again.");

        var group = new CostCentreGroupEntity
        {
            CostCentreGroupId = CommercialIdentifierFactory.NextCostCentreGroupId(),
            ProjectId = command.ProjectId,
            Name = command.Name.Trim()
        };
        context.CostCentreGroups.Add(group);

        foreach (var costCode in codes)
        {
            context.CostCentreGroupMembers.Add(new CostCentreGroupMemberEntity
            {
                CostCentreGroupMemberId = CommercialIdentifierFactory.NextCostCentreGroupMemberId(),
                CostCentreGroupId = group.CostCentreGroupId,
                ProjectId = command.ProjectId,
                CostCode = costCode
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return new CostCentreGroup(group.CostCentreGroupId, group.ProjectId, group.Name,
            codes.OrderBy(costCode => costCode, StringComparer.OrdinalIgnoreCase).ToList());
    }
}
