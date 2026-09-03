using System.Text.RegularExpressions;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Jewel.JPMS.Api.Features.RecordLinks;
using Jewel.JPMS.Contracts.RecordLinks;

namespace Jewel.JPMS.Api.Features.Todos;

// When a to-do is ticked off, the email(s) it was raised from stay filed under the to-do only —
// the record the work produced (the variation that was staged, the work order that was raised)
// has no trace of the email that asked for it. This closes that gap automatically at completion
// (2026-09-03, James): every email tagged to the item is ALSO tagged to each record on the item's
// project that the item or the email NAMES — "Variation V31 for valuation", "VO32: revised
// cladding", "raise WO-0045" — so the record's Communications list shows its source email the
// moment the to-do is done.
//
// Naming is the signal, deliberately: the audit trail and agent-activity log do not say which
// records a piece of work produced, and "everything created on the project since the to-do was
// opened" would tag four variations onto four unrelated emails the day the QS clears his list.
// What is scanned is the item's title and notes plus each email's subject and body preview — the
// short envelope, never the full body, whose "Priced Document Ref V04" style citations would
// pull in unrelated records. A bare "V31" is accepted only directly after the word "variation"
// (so a "Ref V04" rate citation in a preview does not file the email to V4); "VO32" / "VOQ-0032"
// always mean the variation.
//
// Best-effort by design, like TodoEmailActivityRecorder: the item is already complete and saved
// when this runs, and a mailbox hiccup must never turn a ticked box into an error. Each link goes
// through LinkMessageToRecordHandler — the same verified tag write, thread sweep and audit row as
// filing the email by hand in the Control Centre — and a record the email already carries is
// skipped. What was filed is written to the item's timeline so the person can see it happened.
public sealed class TodoCompletionRecordTagger
{
    private readonly JpmsContext context;
    private readonly RecordEmailReader emails;
    private readonly RecordProviderRegistry providers;
    private readonly ICommandHandler<LinkMessageToRecord, Acknowledgement> link;
    private readonly TodoActivityRecorder activity;
    private readonly ILogger<TodoCompletionRecordTagger> logger;

    public TodoCompletionRecordTagger(
        JpmsContext context,
        RecordEmailReader emails,
        RecordProviderRegistry providers,
        ICommandHandler<LinkMessageToRecord, Acknowledgement> link,
        TodoActivityRecorder activity,
        ILogger<TodoCompletionRecordTagger> logger)
    {
        this.context = context;
        this.emails = emails;
        this.providers = providers;
        this.link = link;
        this.activity = activity;
        this.logger = logger;
    }

    // The record types whose references a to-do or its email can name. Order is only the order
    // the timeline line lists them in. Todo itself is excluded (the item's own tag is already on
    // the email; another to-do named in passing is not a record the work produced).
    private static readonly RecordType[] NameableTypes =
    {
        RecordType.Variation,      // lists EVERY stage of the project's variations (VOQ identity pre-approval)
        RecordType.Request,
        RecordType.WorkOrder,
        RecordType.BidPackageInvite,
        RecordType.Defect,
        RecordType.SiteInstruction,
        RecordType.Inventory,
        RecordType.CalendarEvent,
    };

    // Called by UpdateTodoItemHandler AFTER the completion has been saved. actorEmail null = the
    // signed-in user the endpoint stamped (TodoActivityRecorder's default).
    public async Task TagSourceEmailsAsync(TodoItemEntity item, string? actorEmail, CancellationToken cancellationToken)
    {
        // A company-wide item has no project to resolve references against.
        if (string.IsNullOrWhiteSpace(item.ProjectId)) return;

        try
        {
            var messages = await emails.ForRecordAsync(RecordType.Todo, item.TodoItemId, cancellationToken);
            if (messages.Count == 0) return;

            // What the item itself names applies to every one of its emails; what an email names
            // applies to that email alone.
            var itemKeys = new HashSet<string>(StringComparer.Ordinal);
            itemKeys.UnionWith(RecordReferenceScan.Keys(item.Title));
            itemKeys.UnionWith(RecordReferenceScan.Keys(item.Notes));

            var perMessage = new List<(MailboxMessage Message, HashSet<string> Keys)>();
            foreach (var message in messages)
            {
                var keys = new HashSet<string>(itemKeys, StringComparer.Ordinal);
                keys.UnionWith(RecordReferenceScan.Keys(message.Subject));
                keys.UnionWith(RecordReferenceScan.Keys(message.BodyPreview));
                if (keys.Count > 0) perMessage.Add((message, keys));
            }
            if (perMessage.Count == 0) return;

            var wanted = perMessage.SelectMany(pair => pair.Keys).ToHashSet(StringComparer.Ordinal);
            var records = await ResolveAsync(item.ProjectId, wanted, cancellationToken);
            if (records.Count == 0) return;

            var filed = new List<string>();
            foreach (var (message, keys) in perMessage)
            {
                foreach (var key in keys)
                {
                    if (!records.TryGetValue(key, out var record)) continue;

                    // Already filed there (by hand, or by an earlier completion) — nothing to do.
                    var category = TriageCategories.ForRecord(record.TagReference);
                    if (message.Categories.Contains(category, StringComparer.OrdinalIgnoreCase)) continue;

                    try
                    {
                        await link.HandleAsync(
                            new LinkMessageToRecord(message.Id, record.Type, record.RecordId, message.InternetMessageId),
                            cancellationToken);
                        if (!filed.Contains(record.Reference, StringComparer.OrdinalIgnoreCase))
                            filed.Add(record.Reference);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        logger.LogWarning(ex,
                            "To-do {Reference}: its email \"{Subject}\" could not be filed to {Record} on completion.",
                            item.Reference, message.Subject, record.Reference);
                    }
                }
            }

            if (filed.Count == 0) return;

            activity.Record(item, TodoActivityKind.Note,
                $"Source email{(messages.Count == 1 ? "" : "s")} filed to {string.Join(", ", filed)} on completion",
                actorEmail);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "To-do {Reference}: its source emails could not be filed to the records it names.", item.Reference);
        }
    }

    // key ("V:31", "RFI:11", "WO:45") → the project's record carrying that reference, for every
    // wanted key that resolves. One list read per nameable type that has a provider registered.
    private async Task<Dictionary<string, LinkableRecord>> ResolveAsync(
        string projectId, HashSet<string> wanted, CancellationToken cancellationToken)
    {
        var found = new Dictionary<string, LinkableRecord>(StringComparer.Ordinal);
        foreach (var type in NameableTypes)
        {
            if (!providers.TryGet(type, out var provider)) continue;
            var records = await provider.ForProjectAsync(projectId, cancellationToken);
            foreach (var record in records)
            {
                var key = RecordReferenceScan.KeyOfReference(record.Reference);
                if (key is null || !wanted.Contains(key) || found.ContainsKey(key)) continue;
                found[key] = record;
            }
        }
        return found;
    }
}

// The reference grammar people use when they write about records — in a to-do title, an email
// subject, a preview line — normalised to one key per record so "V31", "VO 31", "VOQ-0031" and
// the register's own "VOQ-0031" / "V31" all meet. Numbers compare without leading zeros.
internal static class RecordReferenceScan
{
    // "VO32", "VOQ-0032", "VO 32", "V31" — the variation families. The bare "V" form is gated
    // below on the word "variation" immediately preceding it.
    private static readonly Regex Variation = new(
        @"\bV(?<form>OQ|O)?[-\s]?0*(?<n>\d{1,4})\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // Every other family the portal tags: request kinds, work orders, bid packages, defects, site
    // instructions, inventory items, calendar events. "WO-0045", "RFI 011", "DEF0012".
    private static readonly Regex Other = new(
        @"\b(?<p>RFI|RFA|RFC|RFQ|RFP|NOD|EOT|REQ|WO|BPI|DEF|SI|INV|CAL)[-\s]?0*(?<n>\d{1,5})\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex MentionsVariation = new(
        @"\bvariations?\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static IEnumerable<string> Keys(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) yield break;

        foreach (Match match in Variation.Matches(text))
        {
            // The bare form ("V31") counts only right after the word "variation" ("Variation
            // V31", "variation order V31") — a "Ref V04" rate citation elsewhere in the same
            // sentence is not the record the text is about.
            var bareForm = !match.Groups["form"].Success;
            if (bareForm && !MentionsVariation.IsMatch(text[Math.Max(0, match.Index - 24)..match.Index])) continue;
            if (int.TryParse(match.Groups["n"].Value, out var number) && number > 0)
                yield return $"V:{number}";
        }

        foreach (Match match in Other.Matches(text))
        {
            if (int.TryParse(match.Groups["n"].Value, out var number) && number > 0)
                yield return $"{match.Groups["p"].Value.ToUpperInvariant()}:{number}";
        }
    }

    // A register reference ("VOQ-0031", "V31", "RFI-011", "JBB-2026-002-RFI-011", "WO-0045") →
    // its key, from the trailing prefix + number. Null when the reference has no such tail.
    private static readonly Regex ReferenceTail = new(
        @"(?<p>[A-Za-z]+)-?0*(?<n>\d+)\s*$", RegexOptions.CultureInvariant);

    public static string? KeyOfReference(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) return null;
        var match = ReferenceTail.Match(reference.Trim());
        if (!match.Success || !int.TryParse(match.Groups["n"].Value, out var number) || number <= 0) return null;
        var prefix = match.Groups["p"].Value.ToUpperInvariant();
        if (prefix is "V" or "VO" or "VOQ") prefix = "V";
        return $"{prefix}:{number}";
    }
}
