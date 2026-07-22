using Jewel.JPMS.Models;

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

    // --- Communication pathways (buckets) ---
    // Every triaged thread is filed under exactly one pathway: who the correspondence is with.
    // The pathway is a category tag stamped thread-wide alongside the record tag, so each pathway
    // view is one cheap exact-match Graph filter (same trick as the marker). The hard invariant is
    // the CLIENT WALL: a thread can never carry Client and a non-Client pathway together, so no
    // client-visible surface can ever meet subcontractor or internal correspondence.

    /// <summary>Pathway tag: correspondence with the client side (client, architect).</summary>
    public const string Client = "JPMS/Client";

    /// <summary>Pathway tag: correspondence with subcontractors and suppliers.</summary>
    public const string Subcontractor = "JPMS/Subcontractor";

    /// <summary>Pathway tag: internal Jewel correspondence (to-dos, company admin).</summary>
    public const string Internal = "JPMS/Internal";

    /// <summary>The three pathway tags. Order matters only for display.</summary>
    public static readonly IReadOnlyList<string> AllBuckets = new[] { Client, Subcontractor, Internal };

    /// <summary>True if a category is one of the three pathway (bucket) tags. Bucket tags share the
    /// JPMS/ prefix but are NOT workflow tags for queue-membership purposes: an email carrying only a
    /// bucket has no triage decision, so every "does it have a decision" test must exclude them.</summary>
    public static bool IsBucketTag(string category) =>
        category.Equals(Client, StringComparison.OrdinalIgnoreCase)
        || category.Equals(Subcontractor, StringComparison.OrdinalIgnoreCase)
        || category.Equals(Internal, StringComparison.OrdinalIgnoreCase);

    /// <summary>The pathway a record type files its thread under, or null when the type is
    /// pathway-neutral: a Todo link never sets or changes a pathway, and CostCentre mail can be
    /// valuation-side (Client) or subcontract-side (Subcontractor) — the triager's explicit pathway
    /// choice decides, per email.</summary>
    public static string? BucketFor(RecordType type) => type switch
    {
        RecordType.Request          => Client,
        RecordType.Variation        => Client,
        RecordType.VariationQuote   => Client,
        RecordType.Scheduling       => Client,   // programme correspondence is client/architect-facing
        RecordType.Lad              => Client,   // LAD claims sit between Jewel and the client
        RecordType.BidPackageInvite => Subcontractor,
        RecordType.CostCentre       => null,     // triager picks the side, per email
        RecordType.Todo             => null,     // neutral: never sets or changes a pathway
        _ => null
    };

    /// <summary>True when the two categories are pathway tags on opposite sides of the client wall —
    /// one is <see cref="Client"/> and the other is a non-client pathway. This combination is the one
    /// that is never allowed on a thread, with no override.</summary>
    public static bool CrossesClientWall(string bucketA, string bucketB) =>
        !bucketA.Equals(bucketB, StringComparison.OrdinalIgnoreCase)
        && (bucketA.Equals(Client, StringComparison.OrdinalIgnoreCase)
            || bucketB.Equals(Client, StringComparison.OrdinalIgnoreCase));

    /// <summary>The workflow tag for an email linked to a record, from its reference
    /// (e.g. "RFI-001" -> "JPMS/RFI-001", "BPI-0001" -> "JPMS/BPI-0001"). The record reads its emails
    /// back by this exact tag. Record-type-agnostic: the tag is just the reference stem.</summary>
    public static string ForRecord(string reference) => $"JPMS/{reference.Trim()}";

    /// <summary>Back-compat alias for <see cref="ForRecord"/>, kept while the Request path migrates to
    /// the record-agnostic link layer. Prefer <see cref="ForRecord"/> in new code.</summary>
    public static string ForRequest(string reference) => ForRecord(reference);

    /// <summary>True if a category is a JPMS workflow tag (not the bare marker, not a user category).
    /// Note this includes bucket tags — callers deciding queue membership or "is this thread triaged"
    /// must additionally exclude <see cref="IsBucketTag"/> matches.</summary>
    public static bool IsWorkflowTag(string category) =>
        category.StartsWith(WorkflowPrefix, StringComparison.OrdinalIgnoreCase);
}
