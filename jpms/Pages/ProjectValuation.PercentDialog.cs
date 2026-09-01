using static Jewel.JPMS.MoneyFormats;
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
using Jewel.JPMS.Services.Excel;
using Jewel.JPMS.Services.Navigation;
using Jewel.JPMS.Commercial;
using Jewel.JPMS.Contracts.Ai;
using Jewel.JPMS.Contracts.Commercial;
using Jewel.JPMS.Features.Commercial;

namespace Jewel.JPMS.Pages;

public partial class ProjectValuation
{

    // ---- Set % complete: the dialog ------------------------------------------------------------

    private bool claimProgressOpen;

    private void OpenClaimProgress()
    {
        if (Selected is not { Status: ValuationClaimStatus.Draft }) return;
        claimProgressOpen = true;
    }

    private void CloseClaimProgress()
    {
        claimProgressOpen = false;
    }

    private void ClaimProgressSaved()
    {
        claimProgressOpen = false;
        // The store re-fetched the claim's entries and the claims (totals re-frozen) on save.
    }
}
