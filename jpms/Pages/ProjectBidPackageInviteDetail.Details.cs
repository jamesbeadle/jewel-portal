using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System.Text.Json;
using Microsoft.JSInterop;
using Jewel.JPMS.Components;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Jewel.JPMS.Services;
using Jewel.JPMS.Services.Navigation;
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
    // ---- Package details: the specification summary + the line-item schedule, edited
    // together in ONE dialog. One act of authorship, one save — and the one shape the AI flow
    // can fill in a single update (splitting them across two dialogs relied on the model
    // following through across turns, and it didn't: 2026-08-16). ----

    private bool showDetailsModal;
    private string specDraft = "";
    private List<LineDraft> lineDrafts = new();

    private void EditDetails()
    {
        if (!CanEdit || package is null) return;
        specDraft = package.SpecificationSummary;
        lineDrafts = lineItems
            .Select(item => new LineDraft { Trade = item.Trade, Description = item.Description, Unit = item.Unit, Quantity = item.Quantity, CostCode = item.CostCode })
            .ToList();
        if (lineDrafts.Count == 0) lineDrafts.Add(new LineDraft());
        error = null;
        showDetailsModal = true;
    }

    private void AddLine()
    {
        lineDrafts.Add(new LineDraft());
    }

    private void RemoveLine(LineDraft draft)
    {
        lineDrafts.Remove(draft);
    }

    private void CancelDetails()
    {
        if (busy) return;
        showDetailsModal = false;
        lineDrafts.Clear();
    }

    private async Task SaveDetails()
    {
        if (busy || package is null || !CanEdit) return;
        error = null;
        try
        {
            var kept = lineDrafts
                .Where(draft => !string.IsNullOrWhiteSpace(draft.Description))
                .ToList();
            // Every line put out to tender must know its cost-centre home.
            if (kept.Any(draft => string.IsNullOrWhiteSpace(draft.CostCode)))
            {
                error = "Every line item needs a cost code — pick a cost centre for each line before saving.";
                return;
            }
            busy = true;

            // Summary first, then the schedule — two commands behind one Save. If the second
            // fails the first has still landed; the catch says so and the dialog stays open with
            // everything the user typed.
            package = await Commands.SendAsync(
                new UpdateBidPackageScope(package.BidPackageId, package.Title, package.Trade, package.Status,
                    package.OwnerEmail, package.MaterialsApplicable, specDraft.Trim()),
                CancellationToken.None);

            var inputs = kept
                .Select(draft => new BidPackageLineItemInput(
                    draft.Description.Trim(),
                    (draft.Unit ?? "").Trim(),
                    draft.Quantity,
                    (draft.Trade ?? "").Trim(),
                    draft.CostCode.Trim()))
                .ToList();
            fetchedLineItems = await Commands.SendAsync(new SetBidPackageLineItems(BidPackageId, inputs), CancellationToken.None);

            showDetailsModal = false;
            lineDrafts.Clear();
        }
        catch { error = "Couldn't save the package details — check what's on the record before retrying, the summary may have saved without the lines."; }
        finally { busy = false; }
    }
}
