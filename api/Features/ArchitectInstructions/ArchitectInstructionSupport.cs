using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.ArchitectInstructions;

/// <summary>
/// Who may work with Architect's Instructions. Deliberately wider than the internal-only default:
/// the architect issues the instruction, so the architect can file it themselves rather than
/// emailing it and waiting for someone at Jewel to key it in. Everyone else on the list is a role
/// that already owns the commercial consequence of an instruction.
/// </summary>
internal static class ArchitectInstructionRoles
{
    /// <summary>File, correct, link and delete instructions.</summary>
    public static readonly RoleSet AllowedToManage = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,          // Managing Director
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Architect);

    /// <summary>Read the register and download the documents — everyone who works from them.</summary>
    public static readonly RoleSet AllowedToRead = RoleSet.Of(
        Role.Admin,
        JpmsRoles.Director,
        JpmsRoles.FinanceDirector,
        JpmsRoles.ProjectManager,
        JpmsRoles.Estimator,         // Quantity Surveyor — prices the instructed work
        JpmsRoles.SiteManager,
        JpmsRoles.Foreman,
        JpmsRoles.Architect);
}

internal static class ArchitectInstructionIdentifierFactory
{
    public static string NextArchitectInstructionId() => Guid.NewGuid().ToString("N");
    public static string NextLinkId() => Guid.NewGuid().ToString("N");

    /// <summary>The portal's own per-project reference, rendered AI-0001.</summary>
    public static string Reference(int number) => $"AI-{number:0000}";
}

internal static class ArchitectInstructionMapping
{
    /// <summary>
    /// Entity → model. <paramref name="links"/> is the expanded variation list; pass an empty list
    /// for payloads that deliberately don't carry it. HasFile is derived rather than stored, so a
    /// row whose blob was never written can never claim to have a document.
    /// </summary>
    public static ArchitectInstruction ToModel(
        this ArchitectInstructionEntity entity,
        IReadOnlyList<ArchitectInstructionVariationLink> links) =>
        new(
            entity.ArchitectInstructionId,
            entity.ProjectId,
            entity.Reference,
            entity.InstructionRef,
            entity.Title,
            entity.Notes,
            entity.InstructedAt,
            entity.ReceivedAt,
            entity.IssuedByEmail,
            entity.FiledByEmail,
            (ArchitectInstructionSource)entity.Source,
            entity.FileName,
            entity.ContentType,
            entity.FileSizeBytes,
            !string.IsNullOrWhiteSpace(entity.BlobRef),
            links);

    /// <summary>
    /// Loads the variation links for a set of instructions in one round trip, denormalising each
    /// variation's number, title and status so the register renders without further joins.
    /// </summary>
    public static async Task<Dictionary<string, List<ArchitectInstructionVariationLink>>> LoadLinksAsync(
        JpmsContext context, IReadOnlyCollection<string> instructionIds, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, List<ArchitectInstructionVariationLink>>();
        if (instructionIds.Count == 0) return result;

        var links = await context.ArchitectInstructionVariations
            .AsNoTracking()
            .Where(link => instructionIds.Contains(link.ArchitectInstructionId))
            .ToListAsync(cancellationToken);
        if (links.Count == 0) return result;

        var variationIds = links.Select(link => link.VariationOrderId).Distinct().ToList();
        var variations = await context.VariationOrders
            .AsNoTracking()
            .Where(variation => variationIds.Contains(variation.VariationOrderId))
            .Select(variation => new { variation.VariationOrderId, variation.Number, variation.Title, variation.Status })
            .ToListAsync(cancellationToken);
        var byId = variations.ToDictionary(variation => variation.VariationOrderId);

        foreach (var link in links.OrderBy(link => link.LinkedAt))
        {
            if (!byId.TryGetValue(link.VariationOrderId, out var variation)) continue; // deleted variation
            if (!result.TryGetValue(link.ArchitectInstructionId, out var list))
                result[link.ArchitectInstructionId] = list = new List<ArchitectInstructionVariationLink>();
            list.Add(new ArchitectInstructionVariationLink(
                variation.VariationOrderId,
                $"V{variation.Number}",
                variation.Title,
                (VariationOrderStatus)variation.Status));
        }

        return result;
    }

    /// <summary>Convenience for the single-instruction paths.</summary>
    public static async Task<IReadOnlyList<ArchitectInstructionVariationLink>> LoadLinksForAsync(
        JpmsContext context, string instructionId, CancellationToken cancellationToken)
    {
        var all = await LoadLinksAsync(context, new[] { instructionId }, cancellationToken);
        return all.TryGetValue(instructionId, out var links)
            ? links
            : Array.Empty<ArchitectInstructionVariationLink>();
    }
}
