using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectRequestDetail
{
    private sealed record DraftLineRow(VoqDraftLine Line, bool Accepted, string CostCode = "");

    // The trade for the draft bid package. An explicitly-set trade wins: the assistant is told the
    // field IS the package's trade, so honouring it is both what the schema promises and what makes
    // it round-trip — set Electrical, read Electrical back next turn. Falls back to the first
    // accepted line's trade, which is what this used before the field existed.
    private string DraftPackageTrade
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(draftVariationTrade)) return draftVariationTrade;
            var accepted = draftVariationLines.FirstOrDefault(row => row.Accepted && !string.IsNullOrWhiteSpace(row.Line.Trade));
            return accepted is not null ? accepted.Line.Trade : "General";
        }
    }

    private async Task OpenVariationDraft()
    {
        if (record is null || busy) return;
        variationError = null;
        variationDraftError = null;
        variationDraftOpen = true;

        // Reopening after a cancel restores what was drafted rather than discarding it. Only a
        // first open reseeds the form, from the RFI's own title and description.
        if (!variationDraftSeeded)
        {
            variationDraftSeeded = true;
            draftVariationValue = "";
            draftVariationTrade = "";
            draftVariationLines = new();
            draftVariationTitle = record.Title;
            draftVariationDescription = record.Description;
        }

        // Cost centres feed the scope lines' cost-code selects. Awaited, not fire-and-forget: the
        // select renders disabled until this lands, and nothing was repainting it before.
        try { await CostCenters.RefreshAsync(CancellationToken.None); }
        catch { /* the select stays disabled; a ticked line without a code still blocks the create. */ }
    }

    private void CancelVariationDraft()
    {
        if (busy) return;
        // The draft survives: this closes a window, it does not abandon the work.
        variationDraftOpen = false;
    }

    private async Task ConfirmVariationDraft()
    {
        if (record is null || busy) return;
        variationDraftError = null;

        if (string.IsNullOrWhiteSpace(draftVariationTitle))
        {
            variationDraftError = "A title is required.";
            return;
        }

        decimal? estimatedValue = null;
        if (!string.IsNullOrWhiteSpace(draftVariationValue))
        {
            var raw = draftVariationValue.Replace("£", "").Replace(",", "").Trim();
            if (!decimal.TryParse(raw, out var parsedValue) || parsedValue < 0)
            {
                variationDraftError = "Estimated value must be a number.";
                return;
            }
            estimatedValue = parsedValue;
        }

        // Accepted scope lines become a standalone (draft) bid package on the project — bid packages
        // are separate records from the variation (separation 2026-08-12), so this is the ordinary
        // project package, not a child of the VO. Every line put out to tender must know its
        // cost-centre home, so a ticked line without a code blocks the create.
        var acceptedRows = draftVariationLines.Where(row => row.Accepted).ToList();
        if (acceptedRows.Any(row => string.IsNullOrWhiteSpace(row.CostCode)))
        {
            variationDraftError = "Every ticked scope line needs a cost code — pick a cost centre for each, or untick it.";
            return;
        }

        try
        {
            busy = true;
            variation = await Variations.CreateFromRfqAsync(
                record.RequestId, draftVariationTitle.Trim(), draftVariationDescription.Trim(), estimatedValue);

            if (acceptedRows.Count > 0)
            {
                var package = await Commands.SendAsync(
                    new CreateBidPackage(ProjectId, variation.Title, DraftPackageTrade, Auth.CurrentUser?.Email ?? ""),
                    CancellationToken.None);
                var inputs = acceptedRows
                    .Select(row => new BidPackageLineItemInput(
                        row.Line.Description, row.Line.Unit, row.Line.Quantity,
                        string.IsNullOrWhiteSpace(row.Line.Trade) ? DraftPackageTrade : row.Line.Trade,
                        row.CostCode.Trim()))
                    .ToList();
                await Commands.SendAsync(new AddBidPackageLineItems(package.BidPackageId, inputs), CancellationToken.None);
            }

            variationDraftOpen = false;
            Nav.NavigateTo($"/projects/{ProjectId}/variations/{variation.VariationOrderId}");
        }
        catch
        {
            variationDraftError = variation is null
                ? "Couldn't raise the variation. Please try again."
                : "The variation was raised, but creating the bid package for its scope failed. Add one from the Bid Package Invites tab instead.";
        }
        finally
        {
            busy = false;
        }
    }

    private void OnDraftTitleChanged(string value) => draftVariationTitle = value;

    private void OnDraftDescriptionInput(ChangeEventArgs e) =>
        draftVariationDescription = e.Value?.ToString() ?? string.Empty;

    private void OnDraftValueChanged(string value) => draftVariationValue = value;

    private void OnDraftLineAcceptedChanged(int index, bool accepted)
    {
        if (index < 0 || index >= draftVariationLines.Count) return;
        draftVariationLines[index] = draftVariationLines[index] with { Accepted = accepted };
    }

    private void OnDraftLineCostCodeChanged(int index, string costCode)
    {
        if (index < 0 || index >= draftVariationLines.Count) return;
        draftVariationLines[index] = draftVariationLines[index] with { CostCode = costCode };
    }

    private async Task OnPartyChanged(ChangeEventArgs e)
    {
        if (record is null || busy) return;
        ladderError = null;

        var selection = e.Value?.ToString() ?? "";
        var partyKind = PartyKind.Client;
        string? partyId = null;
        string? onBehalfOfClientId = null;

        if (selection.StartsWith(ArchitectPrefix, StringComparison.Ordinal))
        {
            partyKind = PartyKind.Architect;
            partyId = selection[ArchitectPrefix.Length..];
            // Keep the recorded on-behalf-of client when switching between architects.
            onBehalfOfClientId = record.OnBehalfOfClientId;
        }
        else if (selection.StartsWith(ClientPrefix, StringComparison.Ordinal))
        {
            partyId = selection[ClientPrefix.Length..];
        }

        try
        {
            busy = true;
            record = await RequestRegister.LinkToPartyAsync(record.RequestId, partyKind, partyId, onBehalfOfClientId, ProjectId);
            await LoadRecipientPreviewAsync();
        }
        catch
        {
            ladderError = "Couldn't update the linked party. Please try again.";
        }
        finally
        {
            busy = false;
        }
    }

    private async Task OnOnBehalfOfClientChanged(ChangeEventArgs e)
    {
        if (record is null || busy || string.IsNullOrEmpty(record.PartyId)) return;
        ladderError = null;
        var clientId = e.Value?.ToString();
        if (string.IsNullOrWhiteSpace(clientId)) clientId = null;
        try
        {
            busy = true;
            record = await RequestRegister.LinkToPartyAsync(record.RequestId, PartyKind.Architect, record.PartyId, clientId, ProjectId);
            await LoadRecipientPreviewAsync();
        }
        catch
        {
            ladderError = "Couldn't update the on-behalf-of client. Please try again.";
        }
        finally
        {
            busy = false;
        }
    }

    private async Task Apply(RequestStatus status, string? responseText)
    {
        if (record is null || busy) return;
        actionError = null;
        var email = Auth.CurrentUser?.Email;
        var hasResponse = !string.IsNullOrWhiteSpace(responseText);

        var command = new UpdateRequestDetails(
            record.RequestId,
            record.Reference,
            record.Title,
            record.Description,
            status,
            record.Value,
            hasResponse ? responseText!.Trim() : null,
            hasResponse ? (record.RespondedByEmail ?? email) : record.RespondedByEmail,
            record.ImpliesVariation,
            record.DrawingRef,
            record.ResponseDue,
            record.RelatedDrawingSpec,
            record.InternalNotes,
            record.ClientNotes);

        try
        {
            busy = true;
            record = await RequestRegister.UpdateAsync(command);
            responseDraft = record.ResponseText ?? "";
        }
        catch (CommandFailedException ex)
        {
            actionError = ex.Message;
        }
        catch
        {
            actionError = "Couldn't update the request. Please try again.";
        }
        finally
        {
            busy = false;
        }
    }

    // Flips the Critical Path tag in place from the Actions menu — a one-click alternative to the
    // edit modal. Everything else on the command echoes the record unchanged (and CriticalPath is
    // "keep existing" on every other surface, so only this toggle and the edit modal move it).
    private Task ToggleCriticalPathFromMenu() =>
        SendCriticalPathUpdate(criticalPath: record is { } r ? !r.CriticalPath : (bool?)null, nudgeDismissed: null);

    // The two-week nudge shows on an RFI that is still outstanding a fortnight after issue and
    // carries no answer to the question yet — not tagged, not previously dismissed. The clock is
    // the record's own DaysOutstanding (Issued date, falling back to the created-on stamp), so the
    // banner and the "Days outstanding" fact can never disagree. Closed is gated explicitly here:
    // DaysOutstanding no longer nulls out at Closed (it freezes at the close date so the register
    // keeps the how-long-was-it-out fact), and a closed RFI has nothing left to nudge about.
    private bool ShowCriticalPathNudge =>
        CanEditDetails // both answers write to the request, so viewers aren't asked
        && record is { Kind: RequestType.Rfi, CriticalPath: false, CriticalPathNudgeDismissed: false }
        && record.Status is not RequestStatus.Closed
        && record.DaysOutstanding is >= 14;

    // Yes tags the RFI critical path; No records the dismissal so the nudge never re-asks. Both
    // travel on the same echo command the menu toggle uses.
    private Task AnswerCriticalPathNudge(bool tag) =>
        SendCriticalPathUpdate(criticalPath: tag ? true : (bool?)null, nudgeDismissed: tag ? (bool?)null : true);

    private async Task SendCriticalPathUpdate(bool? criticalPath, bool? nudgeDismissed)
    {
        if (record is null || busy) return;
        actionError = null;

        var command = new UpdateRequestDetails(
            record.RequestId,
            record.Reference,
            record.Title,
            record.Description,
            record.Status,
            record.Value,
            record.ResponseText,
            record.RespondedByEmail,
            record.ImpliesVariation,
            record.DrawingRef,
            record.ResponseDue,
            record.RelatedDrawingSpec,
            record.InternalNotes,
            record.ClientNotes,
            CriticalPath: criticalPath,
            CriticalPathNudgeDismissed: nudgeDismissed);

        try
        {
            busy = true;
            record = await RequestRegister.UpdateAsync(command);
        }
        catch (CommandFailedException ex)
        {
            actionError = ex.Message;
        }
        catch
        {
            actionError = "Couldn't update the critical path tag. Please try again.";
        }
        finally
        {
            busy = false;
        }
    }

}
