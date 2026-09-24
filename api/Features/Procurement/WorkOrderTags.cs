using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Procurement;

/// <summary>
/// Builds the project-qualified mailbox tag stem for a work order. Work-order numbers are minted
/// PER PROJECT (CreateManualWorkOrder / ApproveWorkOrder take MAX within the project, and the
/// migrated Buildertrend orders keep their old PO numbers), so "WO-0045" names one order on By
/// France and another on Coombe Lane — and the JPMS workflow tags share one flat category space in
/// the projects@ mailbox. The stem therefore carries the project, exactly as RequestTags does:
///   stem = "{projectRef}-{reference}"  ->  category "JPMS/JBB-2026-001-WO-0045".
/// The entity's own <see cref="WorkOrderEntity.Reference"/> stays the unqualified reference — what
/// the pre-qualification legacy tag was (2026-09-14, the Coombe Lane collision); what a person reads
/// is <see cref="WorkOrderEntity.ReferenceOn"/>, the same spelling as the stem.
/// </summary>
internal static class WorkOrderTags
{
    /// <summary>The qualified stem from already-loaded parts. Falls back to the (unique) project id
    /// when the project has no human reference yet, so the stem is always project-unique.</summary>
    public static string Stem(string? projectRef, string projectId, string reference) =>
        $"{(string.IsNullOrWhiteSpace(projectRef) ? projectId : projectRef.Trim())}-{reference}";

    /// <summary>The qualified stem for an order, resolving its project's reference.</summary>
    public static async Task<string> StemAsync(JpmsContext context, WorkOrderEntity order, CancellationToken cancellationToken) =>
        Stem(await ProjectRefAsync(context, order.ProjectId, cancellationToken), order.ProjectId, order.Reference);

    /// <summary>The project's human reference (e.g. "JBB-2026-001"), or null when unset.</summary>
    public static Task<string?> ProjectRefAsync(JpmsContext context, string projectId, CancellationToken cancellationToken) =>
        WorkOrderProjectReferences.OfAsync(context, projectId, cancellationToken);

    /// <summary>The order number a stem names — "WO-0045" (the legacy flat stem) or
    /// "JBB-2026-001-WO-0045" — or null when the stem is not a work-order stem at all.</summary>
    public static int? NumberOf(string tagReference)
    {
        var marker = tagReference.LastIndexOf("WO-", StringComparison.OrdinalIgnoreCase);
        if (marker < 0) return null;
        if (marker > 0 && tagReference[marker - 1] != '-') return null;
        return int.TryParse(tagReference[(marker + "WO-".Length)..], out var number) && number > 0 ? number : null;
    }

    /// <summary>True for the pre-qualification flat stem ("WO-0045") — the one that can name an
    /// order on more than one project.</summary>
    public static bool IsLegacyStem(string tagReference) =>
        tagReference.StartsWith("WO-", StringComparison.OrdinalIgnoreCase);
}
