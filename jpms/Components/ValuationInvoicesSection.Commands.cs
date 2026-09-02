namespace Jewel.JPMS.Components;

public partial class ValuationInvoicesSection
{
    private async Task SubmitAsync(ValuationInvoice invoice)
    {
        await RunAsync(() => Invoices.SubmitAsync(invoice.ValuationInvoiceId),
            "Couldn't submit the invoice. Please try again.");
        // A snapshot was frozen — nudge the parent to refresh the report store so the
        // snapshot register shows it.
        if (error is null) await OnCertifiedChanged.InvokeAsync();
    }

    private async Task ApproveAsync(ValuationInvoice invoice)
    {
        await RunAsync(() => Invoices.ApproveAsync(invoice.ValuationInvoiceId),
            "Couldn't approve the invoice. Please try again.");
    }

    private async Task RejectAsync()
    {
        if (busy || rejectInvoice is null) return;
        if (string.IsNullOrWhiteSpace(rejectReason)) { error = "A rejection reason is required."; return; }
        var id = rejectInvoice.ValuationInvoiceId;
        var reason = rejectReason.Trim();
        rejectInvoice = null;
        await RunAsync(() => Invoices.RejectAsync(id, reason),
            "Couldn't record the rejection. Please try again.");
    }

    private async Task CancelAsync(ValuationInvoice invoice)
    {
        await RunAsync(() => Invoices.CancelAsync(invoice.ValuationInvoiceId),
            "Couldn't cancel the invoice. Please try again.");
        // Its snapshots were flagged superseded — refresh the register.
        if (error is null) await OnCertifiedChanged.InvokeAsync();
    }

    private async Task ToggleHistoryAsync(ValuationInvoice invoice)
    {
        if (historyOpenId == invoice.ValuationInvoiceId)
        {
            historyOpenId = null;
            historyEvents = null;
            return;
        }
        historyOpenId = invoice.ValuationInvoiceId;
        historyEvents = null;
        try { historyEvents = await Invoices.ListEventsAsync(invoice.ValuationInvoiceId); }
        catch { historyEvents = Array.Empty<ValuationInvoiceEvent>(); }
    }

    private async Task DeleteAsync(ValuationInvoice invoice)
    {
        if (busy) return;
        error = null;
        var countedTowardCertified = invoice.Status is ValuationInvoiceStatus.Issued or ValuationInvoiceStatus.Paid;
        try
        {
            busy = true;
            await Invoices.DeleteAsync(invoice.ValuationInvoiceId);
            pendingDeleteId = null;
            await ReloadAsync();
            if (countedTowardCertified) await OnCertifiedChanged.InvokeAsync();
        }
        catch { error = "Couldn't delete the valuation invoice. Please try again."; }
        finally { busy = false; }
    }

    private async Task IssueAsync(ValuationInvoice invoice)
    {
        if (busy) return;
        error = null;
        try
        {
            busy = true;
            await Invoices.IssueAsync(invoice.ValuationInvoiceId);
            await ReloadAsync();
            await OnCertifiedChanged.InvokeAsync();
        }
        catch { error = "Couldn't issue the invoice. Please try again."; }
        finally { busy = false; }
    }

    private async Task RecordPaymentAsync()
    {
        if (busy || paymentInvoice is null) return;
        error = null;
        if (!TryParseAmount(paymentAmount, out var amount)) { error = "Enter an amount greater than zero."; return; }
        try
        {
            busy = true;
            await Invoices.RecordPaymentAsync(paymentInvoice.ValuationInvoiceId, amount);
            paymentInvoice = null;
            await ReloadAsync();
        }
        catch { error = "Couldn't record the payment. Please try again."; }
        finally { busy = false; }
    }

    // Shared wrapper for the one-click workflow actions (submit/approve/cancel).
    private async Task RunAsync(Func<Task<ValuationInvoice>> action, string failureMessage)
    {
        if (busy) return;
        error = null;
        try
        {
            busy = true;
            await action();
            await ReloadAsync();
        }
        catch { error = failureMessage; }
        finally { busy = false; }
    }
}
