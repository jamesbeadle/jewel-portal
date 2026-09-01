using Jewel.JPMS.Contracts.MailboxCompose;

namespace Jewel.JPMS.Features.Todos.Detail;

// The communications panel's working state: the tagged-mail list (rendered by the shared
// CorrespondenceThreadList, which owns expansion and full-body fetching itself), one open
// composer at a time (a reply/forward above the list, or the New email form at the top), and
// the outcome note after a send.
public partial class TodoCommunicationsPanel
{
    [Parameter, EditorRequired] public TodoItem Todo { get; set; } = default!;
    /// <summary>Whether the signed-in user may send from the projects mailbox — the API's compose
    /// gate (every internal role), mirrored by the page. Without it the list is read-only.</summary>
    [Parameter] public bool CanSend { get; set; }
    /// <summary>Whether the signed-in user may file an unfiled reply to this item — the API's
    /// triage gate (LinkMessageToRecord), mirrored by the page.</summary>
    [Parameter] public bool CanFile { get; set; }
    /// <summary>Project pool for the composer's drawing / photo attachment sources.</summary>
    [Parameter] public IReadOnlyList<Project> Projects { get; set; } = Array.Empty<Project>();

    private bool loading = true;
    private bool refreshing;
    private string? failed;
    private IReadOnlyList<MailboxMessage> emails = Array.Empty<MailboxMessage>();
    // Bumped on every list read so the unfiled-replies notice re-reads alongside it.
    private int listVersion;

    // One composer at a time: a reply (or forward — composeIsForward says which) above the list,
    // or the new-email form.
    private MailboxMessage? replyingTo;
    private bool composeIsForward;
    private bool composingNew;

    private string? sentNote;
    private string? sentNoteWebLink;

    private string OwnTag => $"JPMS/{Todo.Reference}";

    // The shared thread list renders Reply/Forward only when a delegate is passed — without send
    // rights the callbacks stay empty and the list is read-only, exactly as before.
    private EventCallback<MailboxMessage> ReplyCallback =>
        CanSend ? EventCallback.Factory.Create<MailboxMessage>(this, StartReply) : default;

    private EventCallback<MailboxMessage> ForwardCallback =>
        CanSend ? EventCallback.Factory.Create<MailboxMessage>(this, StartForward) : default;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
        loading = false;
    }

    private async Task LoadAsync()
    {
        failed = null;
        listVersion++;
        try
        {
            emails = (await Todos.ListEmailsAsync(Todo.TodoItemId))
                .OrderByDescending(email => email.ReceivedAt)
                .ToList();
        }
        catch
        {
            failed = "Couldn't read the linked emails. The reason is in the red bar above — refresh to try again.";
        }
    }

    private async Task RefreshAsync()
    {
        if (refreshing) return;
        refreshing = true;
        try { await LoadAsync(); }
        finally { refreshing = false; }
    }

    private void StartReply(MailboxMessage email)
    {
        composingNew = false;
        sentNote = sentNoteWebLink = null;
        replyingTo = email;
        composeIsForward = false;
    }

    private void StartForward(MailboxMessage email)
    {
        composingNew = false;
        sentNote = sentNoteWebLink = null;
        replyingTo = email;
        composeIsForward = true;
    }

    private void StartNewEmail()
    {
        replyingTo = null;
        sentNote = sentNoteWebLink = null;
        composingNew = true;
    }

    private void CloseComposer()
    {
        replyingTo = null;
        composeIsForward = false;
        composingNew = false;
    }

    private async Task HandleSent(ComposeOutcome outcome)
    {
        CloseComposer();
        // The list reads the mailbox by tag, and the sent copy files itself under this item's tag
        // — but Graph can take a moment to surface it, so the note manages that expectation.
        sentNote = outcome.Sent
            ? $"Sent \"{outcome.Subject}\" to {string.Join("; ", outcome.To)}. It can take a moment to appear in this list — refresh if it isn't here yet."
            : outcome.FailureNote
                ?? "The email is saved as a draft in the projects mailbox — review and send it from Outlook.";
        sentNoteWebLink = outcome.WebLink;
        await RefreshAsync();
    }
}
