using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.WeeklyCashflow;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.WeeklyCashflow.Commands;

/// <summary>Creates or rewrites a supplier group. Member names are stored trimmed and
/// de-duplicated case-insensitively — the same comparison the grid groups by, so a name can't
/// sit in a group twice under two spellings of its casing.</summary>
public sealed class SaveWeeklyCashflowSupplierGroupHandler : ICommandHandler<SaveWeeklyCashflowSupplierGroup, WeeklyCashflowSupplierGroup>
{
    private readonly JpmsContext context;

    public SaveWeeklyCashflowSupplierGroupHandler(JpmsContext context) { this.context = context; }

    public async Task<WeeklyCashflowSupplierGroup> HandleAsync(SaveWeeklyCashflowSupplierGroup command, CancellationToken cancellationToken)
    {
        WeeklyCashflowSupplierGroupEntity entity;
        if (command.SupplierGroupId is { } groupId)
        {
            entity = await context.WeeklyCashflowSupplierGroups
                .FirstOrDefaultAsync(group => group.SupplierGroupId == groupId, cancellationToken)
                ?? throw new InvalidOperationException($"Supplier group '{groupId}' not found.");
        }
        else
        {
            entity = new WeeklyCashflowSupplierGroupEntity
            {
                SupplierGroupId = Guid.NewGuid().ToString("N"),
                CreatedByEmail = command.SavedByEmail,
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.WeeklyCashflowSupplierGroups.Add(entity);
        }

        var members = command.ContactNames
            .Select(name => name.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        entity.Name = command.Name.Trim();
        entity.ContactNamesJson = WeeklyCashflowEntityMapping.WriteContactNames(members);

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
