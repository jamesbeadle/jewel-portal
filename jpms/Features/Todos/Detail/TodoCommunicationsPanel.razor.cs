using Jewel.JPMS.Contracts.MailboxCompose;
using Jewel.JPMS.Models;
using Microsoft.AspNetCore.Components;

namespace Jewel.JPMS.Features.Todos.Detail;

// The communications panel's working state: the tagged-mail list, per-email full-body detail
// fetched on demand (cached for the page's life), one open composer at a time (a reply under its
// email, or the New email form at the top), and the outcome note after a send.
public partial class TodoCommunicationsPanel
{
    [Parameter, EditorRequired] public TodoItem Todo { get; set; } = default!;
    /// <summary>Whether the signed-in user may send from the projects mailbox — the API's compose
    /// gate (every internal role), mirrored by the page. Without it the list is read-only.</summary>
    [Parameter] public bool CanSend { get; set; }
    /// <summary>Project pool for the composer's drawing / photo attachment sources.</summary>
    [Parameter] public IReadOnlyList<Project> Projects { get; set; } = Array.Empty<Project>();

    private bool loading = true;
    private bool refreshing;
    private string? failed;
    private IReadOnlyList<MailboxMessage> emails = Array.Empty<MailboxMessage>();

    // Expanded emails render their FULL body (with the quoted thread + attachment names), fetched
    // live from the mailbox and cached per message for the page's life.
    private readonly HashSet<string> expanded = new();
    private readonly Dictionary<string, MailboxMessageDetail?> details = new();

    // One composer at a time: a reply anchored under its email, or the new-email form.
    private MailboxMessage? replyingTo;
    private bool composingNew;

    private string? sentNote;
    private string? sentNoteWebLink;

    private string OwnTag => $"JPMS/{Todo.Reference}";

    private MailboxMessageDetail? DetailFor(string emailId) =>
        details.TryGetValue(emailId, out var detail) ? detail : null;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
        loading = false;
    }

    private async Task LoadAsync()
    {
        failed = null;
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

    private async Task ToggleExpandAsync(MailboxMessage email)
    {
        if (!expanded.Add(email.Id))
        {
            expanded.Remove(email.Id);
            return;
        }
        if (details.ContainsKey(email.Id)) return;

        try
        {
            details[email.Id] = await Intake.GetMessageDetailAsync(email.Id, email.InternetMessageId);
        }
        catch
        {
            // Fetch failed — the card falls back to the preview body inside the expanded pane.
            details[email.Id] = new MailboxMessageDetail(email.Id, "", false, Array.Empty<IntakeAttachment>());
        }
    }

    private void StartReply(MailboxMessage email)
    {
        composingNew = false;
        sentNote = sentNoteWebLink = null;
        replyingTo = email;
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
