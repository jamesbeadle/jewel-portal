using static Jewel.JPMS.MoneyFormats;
using Jewel.JPMS.Contracts.Requests;

namespace Jewel.JPMS.Components;

public partial class RequestConversation
{
    [Parameter, EditorRequired] public string RequestId { get; set; } = "";

    /// <summary>The request's human reference ("RFI-049"). When supplied, the header offers the
    /// Find-emails dialog so more correspondence can be tagged to the request from right here.</summary>
    [Parameter] public string? RequestReference { get; set; }

    /// <summary>Whether the signed-in user may stage reply drafts carrying the official PDF —
    /// mirrors the page-level draft gate (directors, PMs, site managers, architects).</summary>
    [Parameter] public bool CanDraftReply { get; set; }

    private bool loaded;
    private bool busy;
    private string? error;
    private string draft = "";
    private bool internalOnly;
    // Every message on the request (for the header count) and the top-level slice the list
    // renders — replies hang off their parents via repliesByParent instead.
    private IReadOnlyList<RequestMessage> allMessages = Array.Empty<RequestMessage>();
    private IReadOnlyList<RequestMessage> messages = Array.Empty<RequestMessage>();
    private ILookup<string, RequestMessage> repliesByParent =
        Array.Empty<RequestMessage>().ToLookup(m => m.MessageId);
    private string? replyToId;
    private string? replyToAuthor;

    // Inbound emails render their short preview by default; expanding fetches the FULL body (with the
    // quoted thread + attachment names) live from the mailbox, cached per message for the page's life.
    private readonly HashSet<string> expanded = new();
    private readonly Dictionary<string, MailboxMessageDetail?> details = new();

    protected override async Task OnParametersSetAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        allMessages = await RequestRegister.ListMessagesAsync(RequestId);
        var knownIds = allMessages.Select(m => m.MessageId).ToHashSet();
        repliesByParent = allMessages
            .Where(m => m.ParentMessageId is not null && knownIds.Contains(m.ParentMessageId))
            .ToLookup(m => m.ParentMessageId!);
        // Top level, newest first — the latest exchange is what people come to check. A reply
        // whose parent isn't in the list surfaces here rather than disappearing.
        messages = allMessages
            .Where(m => m.ParentMessageId is null || !knownIds.Contains(m.ParentMessageId))
            .OrderByDescending(m => m.PostedAt)
            .ToList();
        loaded = true;
    }

    // The reply subtree under one typed message, oldest first — a thread reads downwards.
    private IReadOnlyList<ConversationEntry> RepliesFor(string messageId) =>
        repliesByParent[messageId]
            .OrderBy(m => m.PostedAt)
            .Select(ToEntry)
            .ToList();

    private ConversationEntry ToEntry(RequestMessage message) => new(
        message.MessageId, message.ParentMessageId, message.AuthorEmail, message.AuthorName,
        message.Body, message.Visibility == MessageVisibility.Internal, message.PostedAt)
    { Replies = RepliesFor(message.MessageId) };

    private void StartReply(RequestMessage message)
    {
        replyToId = message.MessageId;
        replyToAuthor = AuthorLabel(message);
    }

    private void StartReplyEntry(ConversationEntry entry)
    {
        replyToId = entry.MessageId;
        replyToAuthor = string.IsNullOrWhiteSpace(entry.AuthorName) ? entry.AuthorEmail : entry.AuthorName;
    }

    private void CancelReply()
    {
        replyToId = null;
        replyToAuthor = null;
    }

    private void OnDraftInput(ChangeEventArgs e) => draft = e.Value?.ToString() ?? "";

    private void OnVisibilityChanged(ChangeEventArgs e) => internalOnly = e.Value is true;

    private async Task Post()
    {
        if (busy || string.IsNullOrWhiteSpace(draft)) return;
        error = null;
        var user = Auth.CurrentUser;
        if (user is null) { error = "You must be signed in to post."; return; }

        // Author is set authoritatively by the API from the signed-in session; the
        // values supplied here are placeholders the server overrides.
        var command = new PostRequestMessage(
            RequestId,
            draft.Trim(),
            internalOnly ? MessageVisibility.Internal : MessageVisibility.Shared,
            user.Email,
            user.DisplayName,
            replyToId);

        try
        {
            busy = true;
            await RequestRegister.PostMessageAsync(command);
            draft = "";
            internalOnly = false;
            CancelReply();
            await LoadAsync();
        }
        catch
        {
            error = "Couldn't post your message. Please try again.";
        }
        finally
        {
            busy = false;
        }
    }

    private async Task ToggleExpandAsync(RequestMessage message)
    {
        if (!expanded.Add(message.MessageId))
        {
            expanded.Remove(message.MessageId);
            return;
        }

        if (details.ContainsKey(message.MessageId) || string.IsNullOrEmpty(message.MailboxId))
            return;

        try
        {
            var detail = await RequestRegister.GetEmailDetailAsync(RequestId, message.MailboxId, message.EmailMessageId);
            details[message.MessageId] = detail;
        }
        catch
        {
            // Fetch failed — fall back to the preview body inside the expanded pane.
            details[message.MessageId] = new MailboxMessageDetail(message.MailboxId, "", false, Array.Empty<IntakeAttachment>());
        }
    }

    // ---- Reply with the official PDF: an Outlook draft in this email's thread ----

    private string? replyingId;        // message currently being drafted against (disables the buttons)
    private string? replyForMessageId; // where the latest outcome (error or draft) is displayed
    private string? replyError;
    private RequestEmailDraft? replyDraft;

    private async Task DraftReplyAsync(RequestMessage message)
    {
        if (replyingId is not null || string.IsNullOrEmpty(message.MailboxId)) return;
        replyError = null;
        replyDraft = null;
        replyForMessageId = message.MessageId;
        replyingId = message.MessageId;
        try
        {
            replyDraft = await RequestRegister.PrepareReplyDraftAsync(RequestId, message.MailboxId!);
        }
        catch (CommandFailedException ex)
        {
            replyError = $"Couldn't create the reply draft: {ex.Message}";
        }
        catch
        {
            replyError = "Couldn't create the reply draft. Check the mailbox connection and try again.";
        }
        finally
        {
            replyingId = null;
        }
    }

    private static string AuthorLabel(RequestMessage message) =>
        string.IsNullOrWhiteSpace(message.AuthorName) ? message.AuthorEmail : message.AuthorName;

    private int InternalCount => allMessages.Count(m => m.Visibility == MessageVisibility.Internal);


    // The request-scoped attachment stream. Both ids travel so the server can re-find the message
    // by internetMessageId when its Graph id has gone stale — same re-find as the body fetch.
    private string AttachmentUrl(RequestMessage message, IntakeAttachment attachment, bool viewInline) =>
        $"/api/requests/{Uri.EscapeDataString(RequestId)}/messages/email-attachment"
        + $"?id={Uri.EscapeDataString(message.MailboxId ?? "")}"
        + $"&imid={Uri.EscapeDataString(message.EmailMessageId ?? "")}"
        + $"&aid={Uri.EscapeDataString(attachment.Id)}"
        + (viewInline ? "&inline=1" : "");

    // "View" only offers what the endpoint will actually serve inline: raster images and PDFs.
    // (Never SVG — served on the portal's origin it would run the sender's markup as the reader.)
    private static readonly HashSet<string> InlineViewableTypes = new(StringComparer.OrdinalIgnoreCase)
        { "image/png", "image/jpeg", "image/jpg", "image/gif", "image/webp", "image/bmp", "application/pdf" };

    private static bool RendersInBrowser(IntakeAttachment attachment) =>
        attachment.ContentType is { } contentType && InlineViewableTypes.Contains(contentType);

    // Up to two initials for the author avatar: "Nigel Reilly" → NR, "plg@…" → P. Purely a scanning
    // aid, so anything unexpected degrades to a single character rather than throwing.
    private static string Initials(RequestMessage message)
    {
        var label = AuthorLabel(message).Trim();
        if (label.Length == 0) return "?";
        var words = label.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2 && !label.Contains('@'))
            return $"{char.ToUpperInvariant(words[0][0])}{char.ToUpperInvariant(words[^1][0])}";
        return char.ToUpperInvariant(label[0]).ToString();
    }
}
