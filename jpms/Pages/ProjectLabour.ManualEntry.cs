using static Jewel.JPMS.MoneyFormats;
using Jewel.JPMS.Services.Excel;
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
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Contracts.Ai;

namespace Jewel.JPMS.Pages;

public partial class ProjectLabour
{
    // ---- Add a day (manual entry) + the chat's manual_timesheet task ---------------------------

    private void OpenAddDay()
    {
        manualError = null;
        // A full day is the house default — the draft used to publish hours: 0 against the
        // schema's stated minimum of 0.5, which read as a value someone had chosen.
        if (manualHours <= 0) manualHours = 8;
        addDayOpen = true;
    }

    private void CloseAddDay()
    {
        addDayOpen = false;
    }

    private async Task AddManualAsync()
    {
        manualError =
            manualWorkerId == "" ? "Pick which worker the day belongs to."
            : manualCostCode == "" ? "Pick a cost code for the hours."
            : HoursProblem(manualHours);
        if (manualError is not null) return;

        isAddingManual = true;
        try
        {
            await Labour.AddWorkerTimesheetAsync(ProjectId, manualWorkerId, new DateTimeOffset(DateTime.SpecifyKind(manualDate.Date, DateTimeKind.Unspecified), TimeSpan.Zero), manualHours, manualCostCode);
            manualHours = 0;
            addDayOpen = false;
        }
        catch (Exception failure)
        {
            manualError = DescribeFailure(failure, "Could not add the timesheet — check your connection and try again.");
        }
        finally { isAddingManual = false; }
    }

}
