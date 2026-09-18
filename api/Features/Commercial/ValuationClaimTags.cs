using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Commercial;

/// <summary>
/// Builds the project-qualified mailbox tag stem for a valuation — the ONE tag a valuation's
/// correspondence gathers under, the way RequestTags and WorkOrderTags do for theirs:
///   stem = "VAL-{projectRef}-{claimNumber}"  ->  category "JPMS/VAL-JBB-2026-001-4".
///
/// Minted from the per-project ClaimNumber (stable — the period name is renameable). Spelt here
/// and nowhere else: the link provider reads it for the picker and the tag resolver, and the
/// outgoing statement email stamps it, so the sent copy and the client's reply file themselves
/// under the valuation they are about.
///
/// <see cref="RetiredStatementStem"/> is the stem of the RETIRED snapshot object
/// ("VRS-{projectRef}-{number}", consolidated into the claim 2026-09-18). Never stamped on new
/// mail; read only so mail tagged before then still reads as its claim's correspondence.
/// </summary>
internal static class ValuationClaimTags
{
    public const string Prefix = "VAL";
    public const string RetiredStatementPrefix = "VRS";

    /// <summary>The stem from an already-resolved project reference. The one place it is spelt.</summary>
    public static string Stem(string projectReference, int claimNumber) => $"{Prefix}-{projectReference}-{claimNumber}";

    /// <summary>The retired snapshot stem an old email may still carry.</summary>
    public static string RetiredStatementStem(string projectReference, int number) => $"{RetiredStatementPrefix}-{projectReference}-{number}";

    /// <summary>The stem for one claim, resolving its project's reference — falling back to the
    /// (unique) project id when the project has no human reference yet, so the stem stays
    /// project-unique either way, exactly as the register's own reading does.</summary>
    public static async Task<string> StemAsync(
        JpmsContext context, string projectId, int claimNumber, CancellationToken cancellationToken)
        => Stem(await ProjectReferenceAsync(context, projectId, cancellationToken), claimNumber);

    public static async Task<string> ProjectReferenceAsync(JpmsContext context, string projectId, CancellationToken cancellationToken)
    {
        var projectReference = await context.Projects.AsNoTracking()
            .Where(project => project.ProjectId == projectId)
            .Select(project => project.Reference)
            .FirstOrDefaultAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(projectReference) ? projectId : projectReference.Trim();
    }
}
