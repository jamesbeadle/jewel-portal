using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.Requests; // TriageRoles (internal, same assembly)
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Jewel.JPMS.Api.Features.RecordLinks;

// One-off migration sweep for the pathway split (docs/Pathway-Split-Platform-Flow-Plan.md §2,
// Communication-Buckets-Plan "Backfill"): walk every marker-tagged conversation, derive its pathway
// from the record tags it already carries, and stamp the bucket category thread-wide. Idempotent
// (stamp = add-if-missing) and re-runnable; conversations that resolve to BOTH sides of the wall,
// or that carry only cost-centre tags (whose side a human must choose), are REPORTED, never
// guessed. Run with ?dryRun=true first: full per-conversation outcome, no writes.
//
// Mapping (agreed 2026-07-22): request family (RFI/RFA/RFC/RFQ/RFP/NOD/EOT/REQ, project-qualified
// stems) → Client; VO/VOQ → Client; SCH → Client; LAD → Client; BPI → Subcontractor; TODO →
// neutral (a thread whose only record tags are to-dos → Internal); CC → unresolved (per-email
// human choice); Discarded-only → skipped.
public sealed class BackfillBucketsEndpoint
{
    private const int MaxPages = 40;   // 40 × 100 = up to 4,000 tagged messages per run; re-run to continue.
    private const int PageSize = 100;

    private readonly SignedInUserResolver users;
    private readonly IMailboxGraphClient graph;
    private readonly RecordThreadTagger threadTagger;
    private readonly AuditTrail audit;
    private readonly AuditActor auditActor;

    public BackfillBucketsEndpoint(
        SignedInUserResolver users, IMailboxGraphClient graph, RecordThreadTagger threadTagger,
        AuditTrail audit, AuditActor auditActor)
    {
        this.users = users;
        this.graph = graph;
        this.threadTagger = threadTagger;
        this.audit = audit;
        this.auditActor = auditActor;
    }

    public sealed record ConversationOutcome(
        string ConversationId,
        string Subject,
        string Outcome,       // "stamped" | "already-stamped" | "would-stamp" | "conflict" | "unresolved" | "skipped"
        string? Bucket,       // the pathway derived/stamped (short label), when any
        IReadOnlyList<string> Tags,
        string Note);

    public sealed record BackfillReport(
        bool DryRun,
        int ConversationsSeen,
        int Stamped,
        int AlreadyStamped,
        int Conflicts,
        int Unresolved,
        int Skipped,
        bool MorePagesRemain,
        IReadOnlyList<ConversationOutcome> Outcomes);

    [Function("BackfillBuckets")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mailbox/backfill-buckets")] HttpRequest request)
    {
        var signedInUser = await users.ResolveAsync(request, request.HttpContext.RequestAborted);
        if (signedInUser is null) return new UnauthorizedResult();
        if (!TriageRoles.AllowedToTriage.IncludesAny(signedInUser.Roles)) return new StatusCodeResult(403);
        auditActor.Email = signedInUser.Email;

        var dryRun = string.Equals(request.Query["dryRun"].ToString(), "true", StringComparison.OrdinalIgnoreCase);
        var ct = request.HttpContext.RequestAborted;

        // 1. Collect every marker-tagged conversation: its record tags, existing buckets, and an
        //    anchor message to stamp through.
        var conversations = new Dictionary<string, (MailboxMessage Anchor, HashSet<string> Tags, HashSet<string> Buckets)>(StringComparer.Ordinal);
        string? cursor = null;
        var pages = 0;
        bool more;
        do
        {
            var page = await graph.ListTaggedAsync(cursor, PageSize, newestFirst: false, ct);
            foreach (var message in page.Items)
            {
                var key = string.IsNullOrEmpty(message.ConversationId) ? message.Id : message.ConversationId;
                if (!conversations.TryGetValue(key, out var entry))
                {
                    entry = (message, new HashSet<string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                    conversations[key] = entry;
                }
                foreach (var tag in message.Categories) entry.Tags.Add(tag);
                if (!string.IsNullOrEmpty(message.Bucket)) entry.Buckets.Add(message.Bucket!);
            }
            cursor = page.NextCursor;
            pages++;
            more = cursor is not null;
        } while (more && pages < MaxPages);

        // 2. Derive + stamp per conversation.
        var outcomes = new List<ConversationOutcome>();
        int stamped = 0, alreadyStamped = 0, conflicts = 0, unresolved = 0, skipped = 0;
        foreach (var (conversationId, entry) in conversations)
        {
            var recordTags = entry.Tags
                .Where(t => !t.Equals(TriageCategories.Discarded, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (recordTags.Count == 0)
            {
                skipped++;
                outcomes.Add(new ConversationOutcome(conversationId, entry.Anchor.Subject, "skipped", null, entry.Tags.ToList(), "Discarded-only thread — pathways don't apply."));
                continue;
            }

            var derived = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sawCostCentre = false;
            var sawTodo = false;
            foreach (var tag in recordTags)
                switch (BucketForTag(tag))
                {
                    case "CC": sawCostCentre = true; break;
                    case "TODO": sawTodo = true; break;
                    case { } bucket: derived.Add(bucket); break;
                }

            // A thread whose only record links are to-dos is internal work.
            if (derived.Count == 0 && sawTodo && !sawCostCentre)
                derived.Add(TriageCategories.Internal);

            // Fold in buckets already on the thread — a re-run must agree with earlier stamps.
            foreach (var existing in entry.Buckets) derived.Add(existing);

            if (derived.Count > 1)
            {
                conflicts++;
                outcomes.Add(new ConversationOutcome(conversationId, entry.Anchor.Subject, "conflict",
                    null, recordTags,
                    $"Maps to more than one pathway ({string.Join(" + ", derived.Select(AuditTrail.PathwayLabel))}) — resolve by removing the wrong record link, then re-run."));
                continue;
            }
            if (derived.Count == 0)
            {
                unresolved++;
                outcomes.Add(new ConversationOutcome(conversationId, entry.Anchor.Subject, "unresolved",
                    null, recordTags,
                    sawCostCentre
                        ? "Cost-centre mail — the pathway is a per-email choice (valuation-side = Client, subcontract-side = Subcontractor). File it from the Tagged view."
                        : "No record tag implies a pathway. File it from the Tagged view."));
                continue;
            }

            var target = derived.Single();
            if (entry.Buckets.Contains(target))
            {
                alreadyStamped++;
                outcomes.Add(new ConversationOutcome(conversationId, entry.Anchor.Subject, "already-stamped",
                    AuditTrail.PathwayLabel(target), recordTags, ""));
                continue;
            }

            if (dryRun)
            {
                stamped++;
                outcomes.Add(new ConversationOutcome(conversationId, entry.Anchor.Subject, "would-stamp",
                    AuditTrail.PathwayLabel(target), recordTags, ""));
                continue;
            }

            var ok = await threadTagger.TagThreadAsync(
                entry.Anchor.Id, entry.Anchor.InternetMessageId, entry.Anchor.ConversationId, target, ct);
            if (ok)
            {
                stamped++;
                outcomes.Add(new ConversationOutcome(conversationId, entry.Anchor.Subject, "stamped",
                    AuditTrail.PathwayLabel(target), recordTags, ""));
                if (target.Equals(TriageCategories.Client, StringComparison.OrdinalIgnoreCase))
                    await audit.WriteAsync(
                        AuditEventType.BackfillStamped,
                        $"Backfill filed thread \"{Truncate(entry.Anchor.Subject, 120)}\" under Client.",
                        pathway: "Client",
                        conversationId: conversationId,
                        emailMessageId: entry.Anchor.Id,
                        internetMessageId: entry.Anchor.InternetMessageId,
                        cancellationToken: ct);
            }
            else
            {
                unresolved++;
                outcomes.Add(new ConversationOutcome(conversationId, entry.Anchor.Subject, "unresolved",
                    AuditTrail.PathwayLabel(target), recordTags, "The stamp didn't verify — re-run to retry."));
            }
        }

        return new OkObjectResult(new BackfillReport(
            dryRun, conversations.Count, stamped, alreadyStamped, conflicts, unresolved, skipped, more, outcomes));
    }

    // Which pathway a record tag implies. Returns the bucket category, "CC"/"TODO" sentinels for the
    // special cases, or null for tags that imply nothing. Tag shapes (see the providers'
    // ReferencePrefixes): simple stems "JPMS/BPI-0001", "JPMS/TODO-0001", "JPMS/SCH-<proj>",
    // "JPMS/LAD-…", "JPMS/VO-…", "JPMS/VOQ-…", "JPMS/CC-<proj>-<code>"; request stems are
    // PROJECT-QUALIFIED ("JPMS/JBB-2026-001-RFI-012"), so the family prefix appears mid-string.
    private static string? BucketForTag(string tag)
    {
        if (!TriageCategories.IsWorkflowTag(tag) || TriageCategories.IsBucketTag(tag)) return null;
        var stem = tag[TriageCategories.WorkflowPrefix.Length..];

        if (stem.StartsWith("VOQ-", StringComparison.OrdinalIgnoreCase)) return TriageCategories.Client;
        if (stem.StartsWith("VO-", StringComparison.OrdinalIgnoreCase)) return TriageCategories.Client;
        if (stem.StartsWith("SCH-", StringComparison.OrdinalIgnoreCase)) return TriageCategories.Client;
        if (stem.StartsWith("LAD-", StringComparison.OrdinalIgnoreCase)) return TriageCategories.Client;
        if (stem.StartsWith("BPI-", StringComparison.OrdinalIgnoreCase)) return TriageCategories.Subcontractor;
        if (stem.StartsWith("CC-", StringComparison.OrdinalIgnoreCase)) return "CC";
        if (stem.StartsWith("TODO-", StringComparison.OrdinalIgnoreCase)) return "TODO";

        // Request family: the reference may be bare ("RFI-012", legacy) or project-qualified
        // ("JBB-2026-001-RFI-012"), so look for the family prefix as a segment anywhere.
        foreach (var family in new[] { "RFI-", "RFA-", "RFC-", "RFQ-", "RFP-", "NOD-", "EOT-", "REQ-" })
            if (stem.StartsWith(family, StringComparison.OrdinalIgnoreCase)
                || stem.Contains("-" + family, StringComparison.OrdinalIgnoreCase))
                return TriageCategories.Client;

        return null;
    }

    private static string Truncate(string value, int max) =>
        string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max];
}
