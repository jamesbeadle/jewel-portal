using Jewel.JPMS.Features.Triage;

namespace Jewel.JPMS.Pages;

public partial class TriageQueue
{
    // The records the open email's thread tags name, resolved when the email opens so the
    // "Use existing tags" row can say what INV-0021 IS and open it (2026-09-22: the row and its
    // hover listed bare references — nothing a triager could act on). A live read, best effort:
    // a failure or a stem nobody can match leaves that tag as plain text, never an error toast
    // on opening an email. The apply resolves the stems again itself and refuses on a miss.
    private IReadOnlyList<LinkableRecord> threadTagRecords = Array.Empty<LinkableRecord>();

    private async Task LoadThreadTagRecordsAsync(MailboxMessage anchor)
    {
        threadTagRecords = Array.Empty<LinkableRecord>();
        var stems = SelectedThreadTags
            .Select(TriageEmailDisplay.TagLabel)
            .Where(stem => !string.IsNullOrWhiteSpace(stem) && !IsWorkflowTag(stem))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (stems.Count == 0) return;
        try
        {
            var records = await Queries.AskAsync(new ResolveRecordTags(stems), CancellationToken.None);
            // The read is live: if the triager has moved on to another email meanwhile, this
            // answer belongs to the old one and must not land on the new.
            if (!ReferenceEquals(selected, anchor)) return;
            threadTagRecords = records;
        }
        catch
        {
            // The tags read as references alone, exactly as they did before this resolve existed.
        }
    }
}
