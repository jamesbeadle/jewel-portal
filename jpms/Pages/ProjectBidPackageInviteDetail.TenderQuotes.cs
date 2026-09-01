using System.Text.Json;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Pages;

public partial class ProjectBidPackageInviteDetail
{
    // ---- Record a tender submission, review, save as a quote ----

    private sealed class ExtractDraft
    {
        public string? BidPackageLineItemId { get; set; }
        public string Description { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal Total { get; set; }
    }

    private bool showExtractModal;
    private bool extractBusy;
    private bool extractProposed;
    private bool extractComplete;
    private MailboxMessage? extractSourceEmail;
    private List<string> extractIssues = new();
    private string extractSubcontractorNote = "";
    private string extractSubcontractorId = "";
    private string extractNotes = "";
    private List<ExtractDraft> extractDrafts = new();
    private string? awardNote;

    // The one way a tender submission is recorded here — however it arrived (email, phone, post).
    // Saving goes through SaveExtractedQuote, so recipient/package statuses and
    // supersede-on-resubmit behave identically however the prices came in.
    private void OpenManualTenderModal()
    {
        if (busy || !CanEdit) return;
        showExtractModal = true;
        extractBusy = false;
        extractProposed = false;
        extractComplete = false;
        extractSourceEmail = null;
        extractIssues = new();
        extractSubcontractorNote = "";
        extractSubcontractorId = "";
        extractNotes = "";
        extractDrafts = lineItems
            .Select(item => new ExtractDraft { BidPackageLineItemId = item.LineItemId, Description = item.Description, Unit = item.Unit, Quantity = item.Quantity })
            .ToList();
        if (extractDrafts.Count == 0) extractDrafts.Add(new ExtractDraft { Quantity = 1 });
    }


    private void CloseExtractModal() => showExtractModal = false;

    /// <summary>
    /// "Extract information" on a filed tender email: the AI reads the email (body + the returned
    /// pricing-schedule spreadsheet, extracted server-side) against the package's line schedule and
    /// pre-fills this modal with the submission it proposes plus every gap it found. The modal is
    /// the review step — the extraction saves NOTHING, and however the read goes the form falls
    /// back to the blank package schedule so the tender can always be keyed by hand.
    /// </summary>
    private async Task OpenExtractFromEmail(MailboxMessage email)
    {
        if (busy || extractBusy || !CanEdit) return;
        showExtractModal = true;
        extractBusy = true;
        extractProposed = false;
        extractComplete = false;
        extractSourceEmail = email;
        extractIssues = new();
        extractSubcontractorNote = "";
        extractSubcontractorId = "";
        extractNotes = "";
        extractDrafts = new();
        StateHasChanged();
        try
        {
            var proposal = await Commands.SendAsync(
                new ExtractTenderFromMessage(BidPackageId, email.Id), CancellationToken.None);
            extractProposed = proposal.Proposed;
            extractComplete = proposal.Complete;
            extractSubcontractorId = proposal.SubcontractorId ?? "";
            extractSubcontractorNote = proposal.SubcontractorNote;
            extractNotes = proposal.Notes;
            extractIssues = proposal.Issues.ToList();
            extractDrafts = proposal.Lines
                .Select(line => new ExtractDraft
                {
                    BidPackageLineItemId = line.BidPackageLineItemId,
                    Description = line.Description,
                    Unit = line.Unit,
                    Quantity = line.Quantity,
                    Rate = line.Rate,
                    Total = line.Total
                })
                .ToList();
        }
        catch (CommandFailedException ex)
        {
            extractIssues = new List<string> { ex.Message };
        }
        catch
        {
            extractIssues = new List<string> { "The tender couldn't be read just now — enter the submission manually, or close and try again." };
        }
        finally
        {
            if (extractDrafts.Count == 0)
                extractDrafts = lineItems
                    .Select(item => new ExtractDraft { BidPackageLineItemId = item.LineItemId, Description = item.Description, Unit = item.Unit, Quantity = item.Quantity })
                    .ToList();
            if (extractDrafts.Count == 0) extractDrafts.Add(new ExtractDraft { Quantity = 1 });
            extractBusy = false;
            StateHasChanged();
        }
    }

    private void AddExtractLine() => extractDrafts.Add(new ExtractDraft { Quantity = 1 });

    private void RecalcTotal(ExtractDraft draft)
    {
        if (draft.Total == 0 && draft.Rate != 0 && draft.Quantity != 0)
            draft.Total = decimal.Round(draft.Rate * draft.Quantity, 2);
    }

    private async Task ConfirmSaveExtracted()
    {
        if (busy || !CanEdit || string.IsNullOrWhiteSpace(extractSubcontractorId)) return;
        error = null;
        try
        {
            busy = true;
            var lines = extractDrafts
                .Where(draft => !string.IsNullOrWhiteSpace(draft.Description))
                .Select(draft => new QuoteExtractionLine(
                    draft.BidPackageLineItemId, draft.Description.Trim(), (draft.Unit ?? "").Trim(),
                    draft.Quantity, draft.Rate, draft.Total))
                .ToList();
            await Commands.SendAsync(
                new SaveExtractedQuote(BidPackageId, extractSubcontractorId, extractNotes ?? "", lines), CancellationToken.None);
            showExtractModal = false;
            await LoadAsync();
        }
        catch { error = "Couldn't save that submission. Make sure a subcontractor is selected and every line has a description."; }
        finally { busy = false; }
    }

}
