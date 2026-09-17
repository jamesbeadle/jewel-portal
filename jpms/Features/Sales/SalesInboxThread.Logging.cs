using Jewel.JPMS.Contracts.Sales;

namespace Jewel.JPMS.Features.Sales;

public partial class SalesInboxThread
{
    private bool logPickerOpen;

    private async Task LogAsync(MailboxMessage message, SalesInboxLeadMatch match)
    {
        if (busy) return;
        busy = true; actionError = null;
        try
        {
            await Commands.SendAsync(new LogSalesEmailToLead(message.Id, match.LeadId, null), CancellationToken.None);
            await OnDone.InvokeAsync($"Logged on {match.Reference}.");
        }
        catch (CommandFailedException ex) { actionError = ex.Message; }
        finally { busy = false; }
    }

    private void OpenLogPicker()
    {
        actionError = null;
        logPickerOpen = true;
    }

    private async Task LogPickedAsync(string leadId)
    {
        if (busy || string.IsNullOrWhiteSpace(leadId)) return;
        busy = true; actionError = null;
        try
        {
            await Commands.SendAsync(new LogSalesEmailToLead(Message.Id, leadId, null), CancellationToken.None);
            logPickerOpen = false;
            await OnDone.InvokeAsync("Logged on the lead.");
            // The lead now has this sender's email, so the row chips it.
            await RereadListAsync();
        }
        catch (CommandFailedException ex) { actionError = ex.Message; }
        finally { busy = false; }
    }

    private async Task RereadListAsync()
    {
        try { await Inbox.RefreshAsync(Inbox.Cursor, Inbox.Search, CancellationToken.None); }
        catch (OperationCanceledException) { throw; }
        catch { }
    }
}
