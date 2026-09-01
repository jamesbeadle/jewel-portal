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
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Features.RecordLinks;

namespace Jewel.JPMS.Pages;

public partial class ProjectVariations
{
    // ---- Subcontractor variation requests ----

    private IReadOnlyList<SubcontractorVariationRequest> variationRequests = Array.Empty<SubcontractorVariationRequest>();
    private bool requestBusy;
    private string? requestError;
    private string? rejectingRequestId;
    private string rejectReason = "";

    private List<SubcontractorVariationRequest> OpenRequests =>
        variationRequests.Where(r => r.IsOpen).ToList();

    private List<SubcontractorVariationRequest> ReviewedRequests =>
        variationRequests.Where(r => !r.IsOpen).ToList();

    // Mirrors the API's VariationRoles.AllowedToManageVariations.
    private bool CanManageVariations => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.ProjectManager or Role.QuantitySurveyor);

    // Mirrors the API's issue gate (Director/PM, like awarding a bid package).
    private bool CanIssueWorkOrders => Session.AvailableRoles.Any(role =>
        role is Role.Admin or Role.ManagingDirector or Role.ProjectManager);

    private async Task AcceptRequest(string variationRequestId)
    {
        if (requestBusy) return;
        requestError = null;
        try
        {
            requestBusy = true;
            await Variations.AcceptVariationRequestAsync(variationRequestId);
            await LoadVariationsAsync(); // The new Selected variation appears in the register below.
        }
        catch (CommandFailedException ex) { requestError = ex.Message; }
        catch { requestError = "Couldn't accept the request. Please try again."; }
        finally { requestBusy = false; }
    }

    private void StartReject(string variationRequestId)
    {
        rejectingRequestId = variationRequestId;
        rejectReason = "";
        requestError = null;
    }

    private async Task ConfirmReject(string variationRequestId)
    {
        if (requestBusy || string.IsNullOrWhiteSpace(rejectReason)) return;
        requestError = null;
        try
        {
            requestBusy = true;
            await Variations.RejectVariationRequestAsync(variationRequestId, rejectReason.Trim());
            rejectingRequestId = null;
            await LoadVariationsAsync();
        }
        catch (CommandFailedException ex) { requestError = ex.Message; }
        catch { requestError = "Couldn't reject the request. Please try again."; }
        finally { requestBusy = false; }
    }

}
