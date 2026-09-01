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
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Features.Projects;

namespace Jewel.JPMS.Pages;

public partial class XeroAllocation
{

    // -- Xero write-back status -------------------------------------------------

    private sealed record WriteBackBadgeModel(string Text, string? Tooltip, string Css);

    /// <summary>Status line under an allocated line's cost centre(s): what Xero knows.</summary>
    private WriteBackBadgeModel? WriteBackBadge(XeroLedgerLine line) => line.WriteBackStatus switch
    {
        XeroWriteBackStatus.Approved => new("✓ Approved in Xero",
            line.WriteBackAtUtc?.ToLocalTime().ToString("d MMM yyyy HH:mm"), "text-positive"),
        XeroWriteBackStatus.Failed => new("⚠ Xero write-back failed",
            line.WriteBackError, "text-negative"),
        _ when IsAwaitingApproval(line) => new("Draft — approves in Xero once the whole bill is allocated",
            null, "text-content-subtle"),
        _ => null
    };

    private static bool IsAwaitingApproval(XeroLedgerLine line) =>
        line.InvoiceStatus.Equals("DRAFT", StringComparison.OrdinalIgnoreCase)
        || line.InvoiceStatus.Equals("SUBMITTED", StringComparison.OrdinalIgnoreCase);

    private async Task RetryWriteBackAsync(XeroLedgerLine line)
    {
        isApplying = true; errorMessage = null;
        try
        {
            var outcome = await Ledger.RetryWriteBackAsync(line.XeroInvoiceId);
            syncMessage = outcome.Succeeded
                ? $"Invoice {line.InvoiceNumber ?? line.XeroInvoiceId} confirmed and approved in Xero."
                : null;
            errorMessage = outcome.Succeeded ? null : outcome.Error;
        }
        catch (CommandFailedException failure) { errorMessage = failure.Message; }
        finally { isApplying = false; }
    }

}
