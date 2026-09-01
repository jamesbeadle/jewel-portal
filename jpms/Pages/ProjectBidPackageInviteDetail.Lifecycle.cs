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
    // ---- Close / reopen: the no-winner ending. Closing records ClosedAt and makes the page
    // read-only (CanEdit); reopening restores the status the package's data implies. ----

    private async Task ClosePackage()
    {
        if (busy || package is null || !CanManage
            || package.Status is BidPackageStatus.Awarded or BidPackageStatus.Closed) return;
        error = null;
        try
        {
            busy = true;
            package = await Commands.SendAsync(new CloseBidPackage(BidPackageId), CancellationToken.None);
        }
        catch { error = "Couldn't close the bid package. Please try again."; }
        finally { busy = false; }
    }

    private async Task ReopenPackage()
    {
        if (busy || package is null || !CanManage || package.Status != BidPackageStatus.Closed) return;
        error = null;
        try
        {
            busy = true;
            package = await Commands.SendAsync(new ReopenBidPackage(BidPackageId), CancellationToken.None);
        }
        catch { error = "Couldn't reopen the bid package. Please try again."; }
        finally { busy = false; }
    }

    // ---- Delete: removes the record and its tender data for good. Guarded by a confirm modal;
    // the server refuses Awarded packages and anything a work order references. ----

    private bool showDeleteModal;

    private void OpenDeleteModal()
    {
        if (package is null || !CanManage || package.Status == BidPackageStatus.Awarded) return;
        showDeleteModal = true;
    }

    private void CloseDeleteModal()
    {
        if (busy) return;
        showDeleteModal = false;
    }

    private async Task ConfirmDelete()
    {
        if (busy || package is null || !CanManage || package.Status == BidPackageStatus.Awarded) return;
        error = null;
        try
        {
            busy = true;
            await Commands.SendAsync(new DeleteBidPackage(BidPackageId), CancellationToken.None);
            showDeleteModal = false;
            // Back to the register — this record no longer exists to stand on. busy stays true
            // so nothing is clickable during the navigation.
            Nav.NavigateTo($"/projects/{ProjectId}/bid-package-invites");
        }
        catch
        {
            error = "Couldn't delete the bid package — if it has a work order, cancel that first.";
            showDeleteModal = false;
            busy = false;
        }
    }

}
