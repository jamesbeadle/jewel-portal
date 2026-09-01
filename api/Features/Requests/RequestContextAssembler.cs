using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Ganss.Xss;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Features.MailboxIntake;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Requests;

// Gathers everything an agent needs to "see" a request — the request header, its in-app/email
// conversation (RequestMessages) and the originating intake emails — into one text context.
//
// The email legs are read at FULL LENGTH. The list read that finds them (RequestEmailReader → Graph
// $filter) only carries Graph's bodyPreview, which is a ~255-character snippet: it stops mid-sentence
// and drops the quoted thread underneath. Handing that to a model produced exactly the failure you
// would expect — it read "Quarry Architects RFI re", could not see the specification that followed,
// and came back asking the user for something the architect had already written down. So each tagged
// email's real body is fetched on demand here, the same way the conversation panel's "Show full
// email" does, and HTML is flattened to text before it goes anywhere near a prompt.
//
// Attachment NAMES travel with each message. Their contents do not: nothing in this API extracts
// text from a PDF or a drawing. Naming them is still worth doing — it is the difference between an
// agent inventing a specification and an agent saying "the detail is in 2011-RWC-01.pdf, which I
// cannot read; can you tell me what it says?".
public sealed class RequestContextAssembler
{
    /// <summary>
    /// How many tagged emails get their full body fetched. Each is a Graph round trip, and the
    /// assistant's whole turn has ~32 seconds. Beyond this the newest are taken — a long thread's
    /// recent legs are the ones carrying the instruction — and the rest keep their preview, clearly
    /// marked so a reader (human or model) knows the difference.
    /// </summary>
    private const int MaxFullBodyFetches = 25;

    /// <summary>Graph fetches in flight at once. Enough to keep a normal thread inside a second or
    /// two without opening thirty sockets against the mailbox.</summary>
    private const int FetchConcurrency = 6;

    /// <summary>Per-email ceiling before the caller's own budget is applied. A quoted thread twelve
    /// replies deep is mostly repetition, and one runaway email must not crowd out the other five.
    /// </summary>
    private const int MaxBodyChars = 20_000;

    /// <summary>
    /// The floor every message keeps when the budget is shared out. Better six messages at 1,200
    /// characters each — every one of them named, dated, with its subject and attachments — than two
    /// verbatim and four the reader never learns exist.
    /// </summary>
    private const int MinBodyCharsPerMessage = 1_200;

    /// <summary>
    /// Wall clock for the whole Graph fan-out. The assistant checks its 32-second budget only
    /// BETWEEN steps, so nothing inside a tool call is otherwise bounded, and the shared HttpClient
    /// carries the 100-second default. A slow mailbox would take the turn past the gateway and cost
    /// the user a 502; expiring here costs them previews instead, which is a far better trade.
    /// </summary>
    private static readonly TimeSpan FetchDeadline = TimeSpan.FromSeconds(8);

    private readonly JpmsContext context;
    private readonly RequestEmailReader emails;
    private readonly IIntakeMessageReader reader;
    private readonly MailboxIntakeOptions mailboxOptions;
    private readonly ILogger<RequestContextAssembler> logger;

    public RequestContextAssembler(
        JpmsContext context, RequestEmailReader emails,
        IIntakeMessageReader reader, MailboxIntakeOptions mailboxOptions,
        ILogger<RequestContextAssembler> logger)
    {
        this.context = context; this.emails = emails;
        this.reader = reader; this.mailboxOptions = mailboxOptions;
        this.logger = logger;
    }

    /// <param name="maxConversationChars">
    /// The caller's ceiling on the conversation. Applied PER MESSAGE, not by slicing the finished
    /// string: every message keeps its date, author, subject, attachment list and the TOP of its
    /// body, because in an email the new content is at the top and the quoted history below it.
    /// Slicing the assembled text instead would silently drop whole messages and cut the survivor
    /// mid-sentence — the very failure this class exists to fix. Null means no ceiling.
    /// </param>
    public async Task<RequestAgentContext?> AssembleAsync(
        string requestId, CancellationToken cancellationToken, int? maxConversationChars = null)
    {
        var request = await context.Requests
            .FirstOrDefaultAsync(r => r.RequestId == requestId, cancellationToken);
        if (request is null) return null;

        var header = BuildHeader(request);
        var (conversation, trimmed) =
            await BuildConversationAsync(requestId, maxConversationChars, cancellationToken);

        // The conversation already weaves in the emails tagged to this request (read live by tag), so
        // there is no separate intake-email section to assemble.
        return new RequestAgentContext(requestId, header, conversation, "", trimmed);
    }

    private static string BuildHeader(Data.Entities.RequestEntity r)
    {
        var sb = new StringBuilder();
        var number = r.Number > 0 ? $"REQ-{r.Number:0000}" : "(unnumbered)";
        sb.AppendLine($"Number: {number}");
        sb.AppendLine($"Project: {r.ProjectId}");
        sb.AppendLine($"Type: {((RequestType)r.Kind).LongName()}");
        sb.AppendLine($"Reference: {r.Reference}");
        sb.AppendLine($"Title: {r.Title}");
        sb.AppendLine($"Status: {(RequestStatus)r.Status}");
        if (r.Value is not null) sb.AppendLine($"Value: {r.Value:N2}");
        if (!string.IsNullOrWhiteSpace(r.DrawingRef)) sb.AppendLine($"Drawing ref: {r.DrawingRef}");
        if (r.ResponseDue is not null) sb.AppendLine($"Response due: {r.ResponseDue:yyyy-MM-dd}");
        // IssuedAt is the one visible request date; RaisedAt is only the internal created-on stamp
        // (kept as a fallback for rows predating the IssuedAt backfill).
        sb.AppendLine($"Raised by: {r.RaisedByEmail}, issued {(r.IssuedAt ?? r.RaisedAt):yyyy-MM-dd}");
        sb.AppendLine("Description:");
        sb.AppendLine(r.Description);
        if (!string.IsNullOrWhiteSpace(r.ResponseText))
        {
            sb.AppendLine("Response:");
            sb.AppendLine(r.ResponseText);
        }
        return sb.ToString();
    }

    private async Task<(string Text, bool Trimmed)> BuildConversationAsync(
        string requestId, int? maxConversationChars, CancellationToken cancellationToken)
    {
        // In-app activity (notes, drafted-document audit lines) from SQL, plus the emails tagged to
        // this request read live by tag — inbound and the mailbox's own sent replies alike — merged
        // and ordered by time. Legacy stored Inbound rows are excluded; the email legs now come from
        // the mailbox, not a stored copy.
        var stored = await context.RequestMessages
            .Where(m => m.RequestId == requestId && m.Direction != (int)MessageDirection.Inbound)
            .ToListAsync(cancellationToken);

        var live = await emails.ForRequestAsync(requestId, cancellationToken);
        var bodies = await FetchFullBodiesAsync(live, cancellationToken);

        var entries = stored
            .Select(m => m.ToModel())
            .Select(m => new Entry(m.PostedAt, m.AuthorName, m.AuthorEmail, m.Direction.ToString(), null, m.Body, null, false))
            .Concat(live.Select(e =>
            {
                var message = e.ToConversationMessage(requestId, mailboxOptions.Mailbox);
                var full = bodies.TryGetValue(e.Id, out var fetched) ? fetched : null;
                return new Entry(
                    message.PostedAt, message.AuthorName, message.AuthorEmail, message.Direction.ToString(),
                    e.Subject,
                    full?.Body ?? message.Body,
                    full?.Attachments,
                    // No full body means we are back on Graph's ~255-character preview.
                    IsPreviewOnly: full is null,
                    WasClipped: full?.Clipped ?? false);
            }))
            .OrderBy(entry => entry.PostedAt)
            .ToList();

        if (entries.Count == 0) return ("", false);

        var allowances = ShareBudget(entries, maxConversationChars);
        var trimmed = false;

        var sb = new StringBuilder();
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            sb.AppendLine($"[{entry.PostedAt:yyyy-MM-dd HH:mm}] {entry.AuthorName} <{entry.AuthorEmail}> ({entry.Direction}):");
            if (!string.IsNullOrWhiteSpace(entry.Subject))
                sb.AppendLine($"Subject: {entry.Subject}");

            if (entry.Attachments is { Count: > 0 } attachments)
            {
                sb.AppendLine("Attachments (names only — their contents are NOT included and cannot be read): "
                    + string.Join(", ", attachments.Select(Describe)));
            }

            // Said plainly so a model does not treat a fragment as the whole message and reason from
            // half a sentence. This is the failure mode the full-body fetch exists to remove; when it
            // does fall short, the shortfall has to announce itself rather than look like the end.
            if (entry.IsPreviewOnly)
                sb.AppendLine("[NOTE: only a short preview of this email could be retrieved, so it stops mid-way.]");

            // Anything short of the whole message counts, not just the budget cut below: a thread
            // longer than MaxFullBodyFetches, an expired fetch deadline, a failed Graph read and a
            // body clipped at MaxBodyChars are all "you have not seen everything". Reporting only
            // the budget cut would tell a caller the correspondence was complete in precisely the
            // cases where it is worth going back for.
            if (entry.IsPreviewOnly || entry.WasClipped) trimmed = true;

            var body = entry.Body ?? "";
            if (body.Length > allowances[index])
            {
                // Keep the TOP. In an email the new content is above and the quoted history below,
                // so the head is the part that says something new.
                body = body[..allowances[index]]
                    + "\n[NOTE: the rest of this message — mostly the quoted thread below it — was cut to length.]";
                trimmed = true;
            }

            sb.AppendLine(body);
            sb.AppendLine();
        }
        return (sb.ToString().TrimEnd(), trimmed);
    }

    /// <summary>
    /// Divides the caller's budget across the messages: an equal share, then whatever the short
    /// messages did not need handed to the long ones, repeatedly, until nothing more can be given
    /// away. Nobody drops below <see cref="MinBodyCharsPerMessage"/> — a message the reader never
    /// learns exists is worse than one they see the first paragraph of.
    /// </summary>
    private static int[] ShareBudget(IReadOnlyList<Entry> entries, int? budget)
    {
        var allowances = new int[entries.Count];
        if (budget is not { } total)
        {
            for (var index = 0; index < entries.Count; index++) allowances[index] = int.MaxValue;
            return allowances;
        }

        var share = Math.Max(MinBodyCharsPerMessage, total / Math.Max(1, entries.Count));
        for (var index = 0; index < entries.Count; index++)
            allowances[index] = Math.Min(share, entries[index].Body?.Length ?? 0);

        // Hand the surplus round until it stops moving. Bounded by the message count, so a thread of
        // six settles in a few passes and a thread of sixty still terminates.
        for (var pass = 0; pass < entries.Count; pass++)
        {
            var spare = total - allowances.Sum();
            var hungry = new List<int>();
            for (var index = 0; index < entries.Count; index++)
                if ((entries[index].Body?.Length ?? 0) > allowances[index]) hungry.Add(index);

            if (spare <= 0 || hungry.Count == 0) break;

            var extra = spare / hungry.Count;
            if (extra <= 0) break;

            foreach (var index in hungry)
                allowances[index] = Math.Min(allowances[index] + extra, entries[index].Body?.Length ?? 0);
        }

        return allowances;
    }

    private static string Describe(IntakeMessageAttachment attachment) =>
        attachment.Size > 0
            ? $"{attachment.Name} ({attachment.Size / 1024}kB)"
            : attachment.Name;

    /// <summary>
    /// Full body + attachment metadata per email, keyed by Graph id. Best effort throughout: a
    /// message that cannot be fetched simply keeps its preview, because a partial thread beats a
    /// failed page.
    /// </summary>
    private async Task<Dictionary<string, FullBody>> FetchFullBodiesAsync(
        IReadOnlyList<MailboxMessage> live, CancellationToken cancellationToken)
    {
        var results = new Dictionary<string, FullBody>(StringComparer.Ordinal);
        if (live.Count == 0) return results;

        // Newest first when the thread is longer than the budget: the recent legs carry the
        // instruction that made this a variation.
        var wanted = live
            .OrderByDescending(message => message.ReceivedAt)
            .Take(MaxFullBodyFetches)
            .ToList();

        var gate = new SemaphoreSlim(FetchConcurrency);
        var lockObject = new object();
        var failures = 0;

        // The deadline is passed to the Graph reads ONLY, never to the semaphore wait: a fetch that
        // runs out of time falls back to its preview, and the rest of the fan-out carries on.
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(FetchDeadline);

        var fetches = wanted.Select(async message =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                var content = await reader.GetAsync(message.Id, deadline.Token);
                if (content is null) return;

                // One sanitiser per fetch. They run concurrently and the library makes no promise
                // about sharing one; every other call site in this API constructs its own, and it
                // costs nothing next to a Graph round trip.
                var text = content.IsHtml
                    ? HtmlToText(new HtmlSanitizer().Sanitize(content.Body))
                    : content.Body ?? "";
                text = text.Trim();

                var clipped = text.Length > MaxBodyChars;
                if (clipped)
                    text = text[..MaxBodyChars] + "\n[… this email was longer and has been cut here.]";

                if (string.IsNullOrWhiteSpace(text)) return;

                lock (lockObject) results[message.Id] = new FullBody(text, content.Attachments, clipped);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // The fetch deadline, not the caller giving up. Falls back to the preview.
                Interlocked.Increment(ref failures);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One unreadable email must not lose the other five.
                Interlocked.Increment(ref failures);
                logger.LogWarning(ex, "Could not read the full body of message {MessageId}.", message.Id);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(fetches);

        // A mailbox that has quietly started failing every read looks exactly like a healthy one
        // from the outside — the context just gets thinner and the agents get worse.
        if (failures > 0)
        {
            logger.LogWarning(
                "{Failed} of {Total} tagged emails fell back to their preview; the assembled context is short.",
                failures, wanted.Count);
        }

        return results;
    }

    /// <summary>
    /// Flattens sanitised HTML to something a model reads as prose. Block boundaries become
    /// newlines so paragraphs and table rows do not run into one another, tags go, entities are
    /// decoded, and runs of blank lines collapse — Outlook HTML is mostly whitespace.
    /// </summary>
    internal static string HtmlToText(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";

        var text = Regex.Replace(html, @"<(script|style)\b[^>]*>.*?</\1>", " ",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        // <br clear="all"> and <br style="…"> are ordinary Outlook output; matching only a bare
        // <br> would drop them through the generic tag strip below and run two lines together.
        text = Regex.Replace(text, @"<br\b[^>]*>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</(p|div|tr|li|h[1-6]|blockquote|table)\s*>", "\n",
            RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</t[dh]\s*>", "\t", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<[^>]+>", "");
        text = WebUtility.HtmlDecode(text);
        // Outlook litters bodies with non-breaking spaces; left in, they read as odd glyphs.
        text = text.Replace('\u00a0', ' ').Replace("\r\n", "\n").Replace('\r', '\n');
        // Spaces only — the tabs standing in for table cells above have to survive, or a priced
        // schedule collapses into one run of words.
        text = Regex.Replace(text, @"[^\S\t\n]+", " ");
        text = Regex.Replace(text, @" *\n *", "\n");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        return text.Trim();
    }

    private sealed record FullBody(
        string Body, IReadOnlyList<IntakeMessageAttachment> Attachments, bool Clipped);

    private sealed record Entry(
        DateTimeOffset PostedAt,
        string AuthorName,
        string AuthorEmail,
        string Direction,
        string? Subject,
        string Body,
        IReadOnlyList<IntakeMessageAttachment>? Attachments,
        bool IsPreviewOnly,
        /// <summary>The full body came back but was itself longer than MaxBodyChars.</summary>
        bool WasClipped = false);
}
