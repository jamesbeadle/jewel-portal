using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Pages;

public partial class SalesInbox
{
    private bool dataFailed;
    private bool refreshing;
    private string search = "";
    private string? actionNote;

    private MailboxMessage? opened;
    private int openedCount;

    private bool newLeadOpen;
    private Lead? newLeadPrefill;

    protected override async Task OnInitializedAsync()
    {
        Inbox.OnChanged += StateHasChanged;
        Strategies.OnChanged += StateHasChanged;
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        try { await Inbox.RefreshAsync(null, "", CancellationToken.None); }
        catch { dataFailed = true; }
    }

    private async Task RefreshAsync()
    {
        refreshing = true;
        try { await Inbox.RefreshAsync(Inbox.Cursor, search, CancellationToken.None); }
        catch (OperationCanceledException) { throw; }
        catch { }
        finally { refreshing = false; }
    }

    private async Task PageAsync(string? cursor)
    {
        try { await Inbox.RefreshAsync(cursor, search, CancellationToken.None); }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    private async Task OnSearchChanged(string value)
    {
        search = value;
        await PageAsync(null);
    }

    // Every open resets the thread — the reply box closed, only the opened email expanded — so
    // the thread is told the count, not just the message: opening the same email again resets it.
    private void Open(MailboxMessage message)
    {
        opened = message;
        openedCount++;
    }

    private void OpenNewLead(MailboxMessage message)
    {
        // A prefill only — the form captures a new lead from these starting values.
        newLeadPrefill = new Lead("", "", message.FromName, message.FromEmail, "", "", LeadProspectKind.Homeowner, "", "",
            string.IsNullOrWhiteSpace(message.Subject) ? "" : message.Subject, "",
            LeadSource.Inbound, null, null, LeadStage.Contacted, DateTimeOffset.UtcNow, null, Auth.CurrentUser?.Email ?? "",
            DateTimeOffset.UtcNow, null, null, null);
        newLeadOpen = true;
    }

    private async Task OnLeadCreatedAsync(Lead lead)
    {
        newLeadOpen = false;
        actionNote = $"{lead.Reference} captured from the email.";
        if (opened is not null)
        {
            try { await Commands.SendAsync(new LogSalesEmailToLead(opened.Id, lead.LeadId, null), CancellationToken.None); }
            catch (CommandFailedException) { }
        }
        await PageAsync(Inbox.Cursor);
    }

    public void Dispose()
    {
        Inbox.OnChanged -= StateHasChanged;
        Strategies.OnChanged -= StateHasChanged;
    }
}
