using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.RecordLinks;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Pages;

public partial class ProjectVariationDetail
{
    // ---- Originating-request repair --------------------------------------------------------

    // RFIs first — the stage a variation order is normally raised from — then the rest of the
    // register; requests already carrying a variation order are out (a request has at most one).
    private IReadOnlyList<SearchSelect.Option> LinkCandidateOptions
    {
        get
        {
            var taken = projectQuotes
                .Where(quote => !string.IsNullOrWhiteSpace(quote.RequestId) && quote.VariationOrderId != VariationOrderId)
                .Select(quote => quote.RequestId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            return RequestRegister.ForProject(ProjectId)
                .Where(r => !taken.Contains(r.RequestId))
                .OrderBy(r => r.Kind == RequestType.Rfi ? 0 : 1)
                .ThenBy(r => r.Reference, StringComparer.OrdinalIgnoreCase)
                .Select(r => new SearchSelect.Option(r.RequestId, $"{RefLabel(r)} — {r.Title}"))
                .ToList();
        }
    }

    private static string RefLabel(Request record) =>
        string.IsNullOrWhiteSpace(record.Reference) ? record.DisplayNumber : record.Reference;

    private async Task LinkToRequest()
    {
        if (linkBusy || string.IsNullOrWhiteSpace(linkTargetRequestId)) return;
        linkError = null;
        try
        {
            linkBusy = true;
            await Variations.LinkToRequestAsync(VariationOrderId, linkTargetRequestId);
            linkTargetRequestId = "";
            await ReloadAsync(); // the lineage bar re-renders with the request chip in place
        }
        catch (CommandFailedException ex) { linkError = ex.Message; }
        catch { linkError = "Couldn't link the request. Please try again."; }
        finally { linkBusy = false; }
    }

    private void StartRename()
    {
        renameTitle = order?.Title ?? "";
        // Open on a clean slate: a banner left over from an earlier failed save reads as a fresh
        // refusal of the edit that has only just been started.
        error = null;
        renamingOrder = true;
    }

    private void CancelRename()
    {
        renamingOrder = false;
        // The banner belongs to the edit being abandoned — it must not outlive it.
        error = null;
    }

}
