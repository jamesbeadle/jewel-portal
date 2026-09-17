using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Features.Sales;

public partial class SalesInboxThread
{
    [Parameter, EditorRequired] public MailboxMessage Message { get; set; } = default!;
    /// <summary>The page's count of opens — a new one resets the thread, even for the same email.</summary>
    [Parameter] public int Opening { get; set; }
    [Parameter] public string MailboxAddress { get; set; } = "";
    /// <summary>Raised with what just happened, for the page's notice.</summary>
    [Parameter] public EventCallback<string> OnDone { get; set; }
    [Parameter] public EventCallback<MailboxMessage> OnNewLead { get; set; }

    private int openingSeen;
    private bool busy;
    private string? actionError;
    private readonly HashSet<string> expandedIds = new();
    private bool replyOpen;
    private string replyBody = "";

    private bool CanWork => SalesAccess.CanWork(Session.ActiveRole);

    protected override async Task OnParametersSetAsync()
    {
        if (Opening == openingSeen) return;
        openingSeen = Opening;
        actionError = null;
        replyOpen = false;
        replyBody = "";
        expandedIds.Clear();
        expandedIds.Add(Message.Id);
        _ = Inbox.LoadDetailAsync(Message.Id, CancellationToken.None);
        if (string.IsNullOrWhiteSpace(Message.ConversationId) || Inbox.Conversation(Message.ConversationId) is not null) return;
        try { await Inbox.LoadConversationAsync(Message.ConversationId, CancellationToken.None); }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    private async Task ToggleAsync(MailboxMessage message)
    {
        if (!expandedIds.Add(message.Id)) { expandedIds.Remove(message.Id); return; }
        try { await Inbox.LoadDetailAsync(message.Id, CancellationToken.None); }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    private async Task ReplyAsync(MailboxMessage message, SalesInboxLeadMatch? match)
    {
        if (busy || string.IsNullOrWhiteSpace(replyBody)) return;
        busy = true; actionError = null;
        try
        {
            var outcome = await Commands.SendAsync(new ReplyToSalesEmail(message.Id, replyBody.Trim(), match?.LeadId), CancellationToken.None);
            if (!outcome.Sent)
            {
                actionError = outcome.Message + (outcome.DraftWebLink is null ? "" : $" Open the draft: {outcome.DraftWebLink}");
                return;
            }
            await OnDone.InvokeAsync(outcome.Message);
            replyOpen = false;
            replyBody = "";
            await RereadConversationAsync(message.ConversationId);
        }
        catch (CommandFailedException ex) { actionError = ex.Message; }
        finally { busy = false; }
    }

    private async Task RereadConversationAsync(string? conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) return;
        Inbox.ForgetConversation(conversationId);
        try { await Inbox.LoadConversationAsync(conversationId, CancellationToken.None); }
        catch (OperationCanceledException) { throw; }
        catch { }
    }
}
