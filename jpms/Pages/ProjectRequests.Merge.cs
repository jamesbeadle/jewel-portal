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
    // ---- Pre-RFI merge -----------------------------------------------------------------------
    // Exactly two selected General requests can be combined: the chosen survivor keeps its
    // reference/title and absorbs the other's description, conversation, items and emails; the
    // other closes with a "merged into" audit link (visible under Closed, never counted as open).

    private string? mergeSurvivorId;
    private bool merging;
    private string? mergeError;
    private string? mergeResult;

    private string? SurvivorId =>
        SelectedGenerals.Any(r => r.RequestId == mergeSurvivorId)
            ? mergeSurvivorId
            : SelectedGenerals.FirstOrDefault()?.RequestId;

    private static string RefLabel(Request record) =>
        !string.IsNullOrWhiteSpace(record.Reference) ? record.Reference
        : record.DisplayNumber.Length > 0 ? record.DisplayNumber
        : "(no ref)";

    private async Task MergeSelected()
    {
        var generals = SelectedGenerals;
        if (merging || generals.Count != 2) return;

        var survivor = generals.FirstOrDefault(r => r.RequestId == SurvivorId) ?? generals[0];
        var mergedAway = generals.First(r => r.RequestId != survivor.RequestId);

        merging = true;
        mergeError = null;
        mergeResult = null;
        try
        {
            await RequestRegister.MergeAsync(survivor.RequestId, mergedAway.RequestId, ProjectId);
            mergeResult = $"{RefLabel(mergedAway)} merged into {RefLabel(survivor)} — its conversation, queries and emails now live there.";
            selectedIds.Remove(survivor.RequestId);
            selectedIds.Remove(mergedAway.RequestId);
            mergeSurvivorId = null;
        }
        catch (Exception ex)
        {
            mergeError = ex.Message;
        }
        finally
        {
            merging = false;
        }
    }

    private string LabelFor(RequestEmailDraftOutcome outcome) =>
        !string.IsNullOrWhiteSpace(outcome.Reference)
            ? outcome.Reference!
            : AllRecords.FirstOrDefault(r => r.RequestId == outcome.RequestId)?.Reference ?? outcome.RequestId;

    private async Task PrepareSelectedDrafts()
    {
        var rfiIds = SelectedRfis.Select(r => r.RequestId).ToList();
        if (preparingDrafts || rfiIds.Count == 0) return;
        preparingDrafts = true;
        draftBatch = null;
        draftBatchError = null;
        try
        {
            var batch = await RequestRegister.PrepareEmailDraftsAsync(rfiIds);
            draftBatch = batch;
            // Drafted RFIs come off the selection; failures stay ticked for a fix-and-retry.
            foreach (var outcome in batch.Outcomes.Where(o => o.Succeeded))
                selectedIds.Remove(outcome.RequestId);
            // Drafting moved each Open RFI to Awaiting Response server-side (manually set back to
            // Open if a send is cancelled) — revalidate the register so the table shows it.
            if (batch.Outcomes.Any(o => o.Succeeded))
                RequestRegister.Refresh(ProjectId);
        }
        catch (Exception ex)
        {
            draftBatchError = ex.Message;
        }
        finally
        {
            preparingDrafts = false;
        }
    }

}
