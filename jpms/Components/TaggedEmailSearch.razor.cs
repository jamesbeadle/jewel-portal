using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Components;

public partial class TaggedEmailSearch
{
    private const int MinimumQueryLength = 2;
    private const int DebounceMilliseconds = 400;
    private const int MaximumResults = 8;
    private const string TagPrefix = "JPMS/";

    [Parameter, EditorRequired] public string Query { get; set; } = "";

    private string searched = "";
    private string? error;
    private IReadOnlyList<MailboxMessage>? results;
    private IReadOnlyDictionary<string, LinkableRecord> recordsByTag = new Dictionary<string, LinkableRecord>();
    private CancellationTokenSource? debounce;

    // The mailbox search endpoint's own gate (SearchMailboxMessages): mirrors
    // ProjectCommunications.CanTag and TriageQueue.CanTriage; keep the three in step.
    private bool CanSearchMailbox => Session.CanOpen(TriageRoles.AllowedToTriage);

    private bool HasQuery => Query.Trim().Length >= MinimumQueryLength;

    protected override void OnParametersSet()
    {
        var query = Query.Trim();
        if (query == searched) return;
        searched = query;
        debounce?.Cancel();
        results = null;
        error = null;
        if (!CanSearchMailbox || !HasQuery) return;
        var cancellation = debounce = new CancellationTokenSource();
        _ = SearchAsync(query, cancellation.Token);
    }

    private async Task SearchAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(DebounceMilliseconds, cancellationToken);
            var found = await Queries.AskAsync(new SearchMailboxMessages(query, MaximumResults), cancellationToken);
            var records = await ResolveTagsAsync(found, cancellationToken);
            if (cancellationToken.IsCancellationRequested) return;
            results = found;
            recordsByTag = records;
        }
        catch (OperationCanceledException) { return; }
        catch
        {
            if (cancellationToken.IsCancellationRequested) return;
            error = "The mailbox couldn't be searched just then. Try again in a moment.";
        }
        await InvokeAsync(StateHasChanged);
    }

    // The records behind the emails' tags, keyed by tag — a chip links through when its record
    // resolves and reads as the bare stem when it does not (another project's tag, a retired record).
    private async Task<IReadOnlyDictionary<string, LinkableRecord>> ResolveTagsAsync(
        IReadOnlyList<MailboxMessage> emails, CancellationToken cancellationToken)
    {
        var stems = emails.SelectMany(email => email.Categories).Select(TagStem).Distinct().ToList();
        if (stems.Count == 0) return new Dictionary<string, LinkableRecord>();
        var records = await Queries.AskAsync(new ResolveRecordTags(stems), cancellationToken);
        return records
            .GroupBy(record => TagPrefix + record.TagReference, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    private sealed record RecordLink(LinkableRecord Record, string Href);

    private RecordLink? LinkFor(string tag)
    {
        if (!recordsByTag.TryGetValue(tag, out var record)) return null;
        var href = ExplorerRecordTypes.FullPageHref(record);
        return href is null ? null : new RecordLink(record, href);
    }

    private static string TagStem(string tag) =>
        tag.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase) ? tag[TagPrefix.Length..] : tag;

    private static string DisplayFrom(MailboxMessage email) =>
        string.IsNullOrWhiteSpace(email.FromName) ? email.FromEmail : email.FromName;

    public void Dispose()
    {
        debounce?.Cancel();
        debounce?.Dispose();
    }
}
