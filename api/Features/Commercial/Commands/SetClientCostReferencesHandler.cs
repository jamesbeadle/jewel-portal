using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Commercial;

namespace Jewel.JPMS.Api.Features.Commercial.Commands;

/// <summary>
/// Replaces a project's cost centre → client reference map in one save. The map is small (one
/// row per cost centre the project sells against) and always edited as a whole on screen, so a
/// whole-map write is simpler and safer than per-row upserts: what the user saw is what is
/// saved. A blank reference removes the row; a cost centre missing from the entries is removed
/// too. Snapshots already taken keep the reference frozen on their lines — this never rewrites
/// an issued statement, only what the NEXT capture will print.
/// </summary>
public sealed class SetClientCostReferencesHandler
    : ICommandHandler<SetClientCostReferences, IReadOnlyList<ClientCostReference>>
{
    private readonly JpmsContext context;
    public SetClientCostReferencesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<ClientCostReference>> HandleAsync(
        SetClientCostReferences command, CancellationToken cancellationToken)
    {
        var wanted = WantedReferencesByCostCode(command.Entries);
        var existing = await context.ClientCostReferences
            .Where(reference => reference.ProjectId == command.ProjectId)
            .ToListAsync(cancellationToken);

        foreach (var row in existing)
        {
            if (wanted.Remove(row.CostCode, out var clientReference))
                row.ClientReference = clientReference;
            else
                context.ClientCostReferences.Remove(row);
        }

        foreach (var (costCode, clientReference) in wanted)
            context.ClientCostReferences.Add(new ClientCostReferenceEntity
            {
                ClientCostReferenceId = CommercialIdentifierFactory.NextClientCostReferenceId(),
                ProjectId = command.ProjectId,
                CostCode = costCode,
                ClientReference = clientReference
            });

        await context.SaveChangesAsync(cancellationToken);

        var saved = await context.ClientCostReferences.AsNoTracking()
            .Where(reference => reference.ProjectId == command.ProjectId)
            .OrderBy(reference => reference.CostCode)
            .ToListAsync(cancellationToken);
        return saved.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }

    // Trimmed, blank references dropped, one entry per cost centre (last one wins) — the
    // whole-map contract in a shape the loop above can consume directly.
    private static Dictionary<string, string> WantedReferencesByCostCode(IReadOnlyList<ClientCostReferenceEntry> entries)
    {
        var wanted = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            var costCode = entry.CostCode.Trim();
            var clientReference = (entry.ClientReference ?? "").Trim();
            if (clientReference.Length == 0)
                wanted.Remove(costCode);
            else
                wanted[costCode] = clientReference;
        }
        return wanted;
    }
}
