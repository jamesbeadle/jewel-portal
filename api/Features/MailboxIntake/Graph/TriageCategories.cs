namespace Jewel.JPMS.Api.Features.MailboxIntake.Graph;

/// <summary>The categories the triage system stamps on a mailbox message. Triage never moves an
/// email — it tags it — so the Inbox stays whole and each view is a category filter.
///
/// Lives in its own file (rather than MailboxGraphClient.cs) because the worker compiles it too,
/// via a linked include in Jewel.JPMS.Worker.csproj — the outbound draft's categories must be
/// stamped with exactly the same marker and tag stems the API's triage views filter on.</summary>
public static class TriageCategories
{
    /// <summary>The marker present on any email that carries a JPMS workflow tag. The triage queue is
    /// Inbox WITHOUT this; the Tagged view is Inbox WITH it. Graph only filters categories by exact
    /// match (no "starts-with"), so this single marker is how we express "has any JPMS tag".</summary>
    public const string Marker = "JPMS";

    /// <summary>Prefix shared by every workflow tag (e.g. "JPMS/Discarded", "JPMS/RFI-001"). The bare
    /// <see cref="Marker"/> has no trailing slash, so it never matches this — that's how RemoveTag
    /// decides whether any workflow tags remain.</summary>
    public const string WorkflowPrefix = "JPMS/";

    /// <summary>Present on a discarded ("not a request") email.</summary>
    public const string Discarded = "JPMS/Discarded";

    /// <summary>The workflow tag for an email linked to a record, from its reference
    /// (e.g. "RFI-001" -> "JPMS/RFI-001", "BPI-0001" -> "JPMS/BPI-0001"). The record reads its emails
    /// back by this exact tag. Record-type-agnostic: the tag is just the reference stem.</summary>
    public static string ForRecord(string reference) => $"JPMS/{reference.Trim()}";

    /// <summary>Back-compat alias for <see cref="ForRecord"/>, kept while the Request path migrates to
    /// the record-agnostic link layer. Prefer <see cref="ForRecord"/> in new code.</summary>
    public static string ForRequest(string reference) => ForRecord(reference);

    /// <summary>True if a category is a JPMS workflow tag (not the bare marker, not a user category).</summary>
    public static bool IsWorkflowTag(string category) =>
        category.StartsWith(WorkflowPrefix, StringComparison.OrdinalIgnoreCase);
}
