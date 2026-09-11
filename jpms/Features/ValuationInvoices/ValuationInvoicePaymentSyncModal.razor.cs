using Jewel.JPMS.Contracts.ValuationInvoices;

namespace Jewel.JPMS.Features.ValuationInvoices;

public partial class ValuationInvoicePaymentSyncModal
{
    /// <summary>Raised once the sync has run, with a one-line result for the section to show.
    /// The section re-reads its invoices.</summary>
    [Parameter] public EventCallback<string> OnSynced { get; set; }

    private bool open;
    private string projectId = "";
    private ValuationInvoicePaymentSyncPreview? preview;
    private string? error;
    private bool syncing;

    public void Open(string forProjectId)
    {
        projectId = forProjectId;
        preview = null;
        error = null;
        open = true;
        _ = LoadPreviewAsync();
        StateHasChanged();
    }

    private void Close()
    {
        if (syncing) return;
        open = false;
    }

    private string ConfirmLabel =>
        syncing ? "Syncing…"
        : preview is null || !preview.HasWork ? "Nothing to record"
        : preview.PaymentsPlanned > 0
            ? $"Record {preview.PaymentsPlanned} {(preview.PaymentsPlanned == 1 ? "payment" : "payments")}"
              + (preview.LinksPlanned > preview.PaymentsPlanned ? $" and link {preview.LinksPlanned - preview.PaymentsPlanned}" : "")
            : $"Link {preview.LinksPlanned} {(preview.LinksPlanned == 1 ? "invoice" : "invoices")}";

    private async Task LoadPreviewAsync()
    {
        try { preview = await Invoices.PreviewPaymentSyncAsync(projectId); error = null; }
        catch (CommandFailedException ex) { error = ex.Message; }
        catch { error = "Couldn't read the project's sales invoices from Xero. Please try again."; }
        finally { StateHasChanged(); }
    }

    private async Task SyncAsync()
    {
        if (syncing || preview is null || !preview.HasWork) return;
        error = null;
        try
        {
            syncing = true;
            var outcome = await Invoices.SyncPaymentsFromXeroAsync(projectId);
            open = false;
            await OnSynced.InvokeAsync(ResultLine(outcome));
        }
        catch (CommandFailedException ex) { error = $"Couldn't sync payments: {ex.Message}"; }
        catch { error = "Couldn't sync payments from Xero. Please try again."; }
        finally { syncing = false; }
    }

    private static string ResultLine(ValuationInvoicePaymentSyncOutcome outcome)
    {
        var recorded = outcome.Results
            .Where(result => result.Applied && result.Row.RecordsPayment)
            .Select(result => $"{result.Row.Reference} ({result.Row.XeroInvoiceNumber})")
            .ToList();
        var line = $"Synced from Xero: {outcome.PaymentsRecorded} {(outcome.PaymentsRecorded == 1 ? "payment" : "payments")} recorded"
            + (recorded.Count > 0 ? $" — {string.Join(", ", recorded)}" : "")
            + $", {outcome.Linked} linked, {outcome.NoChange} unchanged.";
        if (outcome.Failed > 0)
            line += $" {outcome.Failed} could not be applied: "
                + string.Join("; ", outcome.Results.Where(result => result.Error is not null).Select(result => $"{result.Row.Reference}: {result.Error}"));
        return line;
    }

    private static string ActionLabel(ValuationInvoicePaymentSyncAction action) => action switch
    {
        ValuationInvoicePaymentSyncAction.Link => "Link",
        ValuationInvoicePaymentSyncAction.RecordPayment => "Record payment",
        ValuationInvoicePaymentSyncAction.LinkAndRecordPayment => "Link + record payment",
        _ => "No change"
    };

    private static Tone ToneOf(ValuationInvoicePaymentSyncAction action) => action switch
    {
        ValuationInvoicePaymentSyncAction.RecordPayment or ValuationInvoicePaymentSyncAction.LinkAndRecordPayment => Tone.Positive,
        ValuationInvoicePaymentSyncAction.Link => Tone.Info,
        _ => Tone.Muted
    };
}
