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

        // Groups being merged into the new one are dissolved in the same save — this is
        // how an existing roll-up is grown without ungrouping and starting over.
        var replaceIds = (command.ReplaceGroupIds ?? Array.Empty<string>())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (replaceIds.Count > 0)
        {
            var groupsToReplace = await context.CostCentreGroups
                .Where(group => group.ProjectId == command.ProjectId && replaceIds.Contains(group.CostCentreGroupId))
                .ToListAsync(cancellationToken);
            if (groupsToReplace.Count != replaceIds.Count)
                throw new InvalidOperationException("One of the groups being merged no longer exists — refresh and try again.");

            var membersToReplace = await context.CostCentreGroupMembers
                .Where(member => replaceIds.Contains(member.CostCentreGroupId))
                .ToListAsync(cancellationToken);
            context.CostCentreGroupMembers.RemoveRange(membersToReplace);
            context.CostCentreGroups.RemoveRange(groupsToReplace);
        }

        // A centre can only sit in one roll-up per project — the unique index is the real
        // guarantee, but check up-front so the user gets a friendly rejection, not a 500.
        // Members of the groups being replaced don't count: their rows go in this save.
        var alreadyGrouped = await context.CostCentreGroupMembers
            .Where(member => member.ProjectId == command.ProjectId
                             && codes.Contains(member.CostCode)
                             && !replaceIds.Contains(member.CostCentreGroupId))
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
