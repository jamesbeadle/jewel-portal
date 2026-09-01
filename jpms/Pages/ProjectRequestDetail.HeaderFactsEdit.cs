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
    // ---- Header edit: reference, status, subject (+ critical path, + close date when closing) --

    private void OpenHeaderEdit()
    {
        if (record is null) return;
        editReference = record.Reference;
        editTitle = record.Title;
        editStatus = record.Status;
        editClosedAt = record.ClosedAt?.LocalDateTime.ToString("yyyy-MM-dd") ?? "";
        editCriticalPath = record.CriticalPath;
        editError = null;
        editingHeader = true;
    }

    private void CancelHeaderEdit() => editingHeader = false;

    private void OnEditStatusChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var raw)) editStatus = (RequestStatus)raw;
        // Switching to Closed pre-fills today so the common case is one click — the date stays
        // editable for closures recorded after the fact, and save refuses a blank one.
        if (editStatus == RequestStatus.Closed && string.IsNullOrWhiteSpace(editClosedAt))
            editClosedAt = DateTime.Today.ToString("yyyy-MM-dd");
    }

    private void OnEditDescriptionInput(ChangeEventArgs e) => editDescription = e.Value?.ToString() ?? "";

    private async Task SaveHeaderEdit()
    {
        if (record is null || busy || !CanEditDetails) return;
        editError = null;

        if (string.IsNullOrWhiteSpace(editReference)) { editError = "A reference is required."; return; }
        if (string.IsNullOrWhiteSpace(editTitle)) { editError = "A subject is required."; return; }

        // The close date only travels when the record is closed; backdating is fine, forward-dating
        // isn't, and a blank is refused — left to the server it would silently stamp today, which
        // misdates closures recorded after the fact and skews the frozen Days-out count.
        var closedAt = editStatus == RequestStatus.Closed ? ParseDate(editClosedAt) : null;
        if (editStatus == RequestStatus.Closed && closedAt is null) { editError = "A closed date is required — the days-out count is fixed from it."; return; }
        if (closedAt is { } closed && closed.Date > DateTime.Today) { editError = "The closed date cannot be in the future."; return; }

        var command = new UpdateRequestDetails(
            record.RequestId,
            editReference.Trim(),
            editTitle.Trim(),
            record.Description,
            editStatus,
            record.Value,
            record.ResponseText,
            record.RespondedByEmail,
            record.ImpliesVariation,
            record.DrawingRef,
            record.ResponseDue,
            record.RelatedDrawingSpec,
            record.InternalNotes,
            record.ClientNotes,
            ClosedAt: closedAt,
            // Only the RFI modal shows the checkbox; null keeps the tag untouched elsewhere.
            CriticalPath: record.Kind is RequestType.Rfi ? editCriticalPath : null);

        // e.g. the reference was manually edited onto a number already in use on this project.
        if (await SendEdit(command)) editingHeader = false;
    }

    // ---- Facts edit: the at-a-glance strip (dates, drawing refs, value) -------------------------

    private void OpenFactsEdit()
    {
        if (record is null) return;
        // The one visible date; rows predating the backfill show the created-on stamp.
        editIssuedAt = (record.IssuedAt ?? record.RaisedAt).LocalDateTime.ToString("yyyy-MM-dd");
        editResponseDue = record.ResponseDue?.LocalDateTime.ToString("yyyy-MM-dd") ?? "";
        editClosedAt = record.ClosedAt?.LocalDateTime.ToString("yyyy-MM-dd") ?? "";
        editDrawingRef = record.DrawingRef ?? "";
        editRelatedSpec = record.RelatedDrawingSpec ?? "";
        editValue = record.Value?.ToString("0.##") ?? "";
        editError = null;
        editingFacts = true;
    }

    private void CancelFactsEdit() => editingFacts = false;

    private async Task SaveFactsEdit()
    {
        if (record is null || busy || !CanEditDetails) return;
        editError = null;

        // The issue date records when the document actually went out — today or earlier.
        var issuedAt = ParseDate(editIssuedAt);
        if (issuedAt is { } issuedDate && issuedDate.Date > DateTime.Today) { editError = "The issued date cannot be in the future."; return; }

        // The close date is offered on every closed record — the way to backfill legacy closes
        // that predate the field. Required while closed: a blank sent to the server would silently
        // stamp today, misdating the close and skewing the frozen Days-out count. Never a future one.
        var closedAt = record.Status is RequestStatus.Closed ? ParseDate(editClosedAt) : null;
        if (record.Status is RequestStatus.Closed && closedAt is null) { editError = "A closed date is required — the days-out count is fixed from it."; return; }
        if (closedAt is { } closed && closed.Date > DateTime.Today) { editError = "The closed date cannot be in the future."; return; }

        var value = record.Value;
        if (record.Kind is not RequestType.Rfi)
        {
            if (string.IsNullOrWhiteSpace(editValue))
            {
                value = null;
            }
            else
            {
                var raw = editValue.Replace("£", "").Replace(",", "").Trim();
                if (!decimal.TryParse(raw, out var parsedValue) || parsedValue < 0)
                {
                    editError = "Value must be a number.";
                    return;
                }
                value = parsedValue;
            }
        }

        var command = new UpdateRequestDetails(
            record.RequestId,
            record.Reference,
            record.Title,
            record.Description,
            record.Status,
            value,
            record.ResponseText,
            record.RespondedByEmail,
            record.ImpliesVariation,
            NullIfBlank(editDrawingRef),
            ParseDate(editResponseDue),
            NullIfBlank(editRelatedSpec),
            record.InternalNotes,
            record.ClientNotes,
            ClosedAt: closedAt,
            IssuedAt: issuedAt);

        if (await SendEdit(command)) editingFacts = false;
    }

}
