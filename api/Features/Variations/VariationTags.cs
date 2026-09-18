using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Variations;

/// <summary>
/// Builds the project-qualified mailbox tag stem for a variation, the way RequestTags and
/// WorkOrderTags do for theirs. A variation is ONE document that changes identity part way
/// through: the V-ref is minted at approval, so until then its mail hangs off the stable quoting
/// stem and after it off the contract-stage one.
///   VariationRef null  ->  "VOQ-{projectRef}-{number:0000}"
///   otherwise          ->  "VO-{projectRef}-{variationRef}"
/// Getting that choice wrong is not cosmetic: a guessed V-ref would silently detach the mail the
/// moment the real one lands, which is why the rule lives in one place that the link provider and
/// the outgoing email both read.
/// </summary>
internal static class VariationTags
{
    public static string Stem(string projectReference, string? variationRef, int number) =>
        variationRef is null
            ? QuotingStem(projectReference, number)
            : ContractStem(projectReference, variationRef, number);

    /// <summary>The stem for one variation, resolving its project's reference — falling back to
    /// the (unique) project id when the project has no human reference yet, so the stem stays
    /// project-unique either way, exactly as the register's own reading does.</summary>
    public static async Task<string> StemAsync(
        JpmsContext context, VariationOrderEntity entity, CancellationToken cancellationToken)
    {
        var projectReference = await context.Projects.AsNoTracking()
            .Where(project => project.ProjectId == entity.ProjectId)
            .Select(project => project.Reference)
            .FirstOrDefaultAsync(cancellationToken);
        var qualifier = string.IsNullOrWhiteSpace(projectReference) ? entity.ProjectId : projectReference.Trim();
        return Stem(qualifier, entity.VariationRef, entity.Number);
    }

    private static string QuotingStem(string projectReference, int number) =>
        $"VOQ-{projectReference}-{number:0000}";

    /// <summary>A variation approved before the V-ref was stamped reads by its number, which is
    /// what the register shows for it too.</summary>
    private static string ContractStem(string projectReference, string variationRef, int number) =>
        $"VO-{projectReference}-{(string.IsNullOrWhiteSpace(variationRef) ? $"V{number:00}" : variationRef.Trim())}";
}
