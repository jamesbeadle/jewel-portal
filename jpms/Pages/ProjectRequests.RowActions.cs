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
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectRequests
{
    // ---- Bulk selection ----------------------------------------------------------------------
    // One selection set serves two bulk actions: email drafts (RFIs — a General request has no
    // official document to send yet) and pre-RFI merging (open General requests). Selection
    // survives filter/tab switches within the page, so someone can tick rows across views.

    private readonly HashSet<string> selectedIds = new();
    private bool preparingDrafts;
    private string? draftBatchError;
    private RequestEmailDraftBatch? draftBatch;

    // Mirrors PrepareRequestEmailDraftsAuthorisation server-side (directors, project managers,
    // site managers and architects; admins carry every role server-side).
    private bool CanDraftEmail => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.ProjectManager or Role.SiteManager or Role.Architect);

    // Mirrors MergeRequestsAuthorisation server-side (admins, directors and project managers).
    private bool CanMerge => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.ProjectManager);

    // ---- In-row status changes (the chip's dropdown) --------------------------------------------

    // Mirrors the detail page's CanEditDetails (and UpdateRequestDetailsAuthorisation server-side):
    // project managers and administrators.
    private bool CanChangeStatus => Session.AvailableRoles.Any(role => role is Role.Admin or Role.ProjectManager);

    private string? statusBusyRequestId;
    private string? statusError;

    // Same routing as the detail page's status pill, adapted for one-click register use: Closed
    // closes as at today (the record's own close flow remains the place to backdate); everything
    // else applies directly, keeping the recorded response text.
    private async Task ChangeRequestStatus((Request Record, RequestStatus Status) change)
    {
        var (record, status) = change;
        if (statusBusyRequestId is not null || status == record.Status) return;
        statusError = null;
        try
        {
            statusBusyRequestId = record.RequestId;

            if (status == RequestStatus.Closed)
            {
                var closed = await RequestRegister.CloseAsync(record.RequestId, record.ProjectId, DateTimeOffset.Now);
                if (!closed)
                    statusError = $"{RowRef(record)} couldn't be closed — it no longer exists.";
                return;
            }

            var hasResponse = !string.IsNullOrWhiteSpace(record.ResponseText);
            await RequestRegister.UpdateAsync(new UpdateRequestDetails(
                record.RequestId,
                record.Reference,
                record.Title,
                record.Description,
                status,
                record.Value,
                record.ResponseText,
                hasResponse ? (record.RespondedByEmail ?? Auth.CurrentUser?.Email) : record.RespondedByEmail,
                record.ImpliesVariation,
                record.DrawingRef,
                record.ResponseDue,
                record.RelatedDrawingSpec,
                record.InternalNotes,
                record.ClientNotes));
        }
        catch (CommandFailedException ex)
        {
            statusError = $"{RowRef(record)}: {ex.Message}";
        }
        catch
        {
            statusError = $"Couldn't change the status of {RowRef(record)}. Please try again.";
        }
        finally
        {
            statusBusyRequestId = null;
        }
    }

    private static string RowRef(Request record) =>
        string.IsNullOrWhiteSpace(record.Reference) ? record.DisplayNumber : record.Reference;

    private static bool IsRfi(Request record) => record.Kind == RequestType.Rfi;

    // Open General requests that haven't already been merged away — the merge candidates.
    private static bool IsMergeableGeneral(Request record) =>
        record.Kind == RequestType.General
        && record.MergedIntoRequestId is null
        && record.Status is not RequestStatus.Closed;

    private bool IsSelectableRow(Request record) =>
        (CanDraftEmail && IsRfi(record)) || (CanMerge && IsMergeableGeneral(record));

    private List<Request> SelectedRfis =>
        AllRecords.Where(r => selectedIds.Contains(r.RequestId) && IsRfi(r)).ToList();

    private List<Request> SelectedGenerals =>
        AllRecords.Where(r => selectedIds.Contains(r.RequestId) && IsMergeableGeneral(r)).ToList();

    private void ToggleSelect(Request record)
    {
        if (!IsSelectableRow(record)) return;
        if (!selectedIds.Remove(record.RequestId)) selectedIds.Add(record.RequestId);
    }

    // The header checkbox acts on the selectable rows currently shown: ticking it adds them all,
    // unticking removes them — without touching selections made under other filters.
    private void ToggleSelectAll(bool select)
    {
        var visible = FilteredRecords.Where(IsSelectableRow).Select(r => r.RequestId).ToList();
        if (select) selectedIds.UnionWith(visible);
        else selectedIds.ExceptWith(visible);
    }

    private void ClearSelection()
    {
        selectedIds.Clear();
        draftBatch = null;
        draftBatchError = null;
        mergeSurvivorId = null;
        mergeError = null;
        mergeResult = null;
    }

}
