using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Xero;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jewel.JPMS.Api.Features.Xero.Ledger;

/// <summary>
/// Decides when an invoice's allocation is confirmed back to Xero and records what
/// happened. An invoice is written back once EVERY stored line of it is Allocated
/// (a project + cost centre(s) — bucketed/ignored lines block auto-approval, since
/// approving a bill whose lines were waved through unallocated defeats the point)
/// AND the invoice is still awaiting approval in Xero (DRAFT/SUBMITTED). Invoices
/// approved outside JPMS keep flowing through allocation portal-side only.
///
/// Write-back is best-effort by design: the JPMS allocation always saves first and
/// survives regardless; a failed write-back is stamped on the lines (visible on the
/// allocation page) and can be retried without re-allocating.
/// </summary>
public interface IXeroWriteBackService
{
    /// <summary>Attempts write-back for each invoice that has just become eligible. Never throws.</summary>
    Task TryWriteBackAsync(IReadOnlyCollection<string> xeroInvoiceIds, CancellationToken ct);

    /// <summary>Explicit re-attempt for one invoice (the allocation page's Retry button).</summary>
    Task<XeroWriteBackOutcome> RetryAsync(string xeroInvoiceId, CancellationToken ct);
}

public sealed class XeroWriteBackService : IXeroWriteBackService
{
    // Only invoices still awaiting approval in Xero are written back.
    private static readonly string[] AwaitingApprovalStatuses = { "DRAFT", "SUBMITTED" };

    private readonly IXeroClient xero;
    private readonly JpmsContext context;
    private readonly ILogger<XeroWriteBackService> logger;

    public XeroWriteBackService(IXeroClient xero, JpmsContext context, ILogger<XeroWriteBackService> logger)
    {
        this.xero = xero;
        this.context = context;
        this.logger = logger;
    }

    public async Task TryWriteBackAsync(IReadOnlyCollection<string> xeroInvoiceIds, CancellationToken ct)
    {
        foreach (var invoiceId in xeroInvoiceIds.Distinct())
        {
            try
            {
                await WriteBackInvoiceAsync(invoiceId, explicitRetry: false, ct);
            }
            catch (Exception unexpected)
            {
                // Never let a write-back hiccup undo or fail the allocation itself.
                logger.LogError(unexpected, "Xero write-back failed unexpectedly for invoice {InvoiceId}.", invoiceId);
            }
        }
    }

    public async Task<XeroWriteBackOutcome> RetryAsync(string xeroInvoiceId, CancellationToken ct)
    {
        try
        {
            return await WriteBackInvoiceAsync(xeroInvoiceId, explicitRetry: true, ct);
        }
        catch (Exception unexpected)
        {
            logger.LogError(unexpected, "Xero write-back retry failed unexpectedly for invoice {InvoiceId}.", xeroInvoiceId);
            return new XeroWriteBackOutcome(false, unexpected.Message);
        }
    }

    private async Task<XeroWriteBackOutcome> WriteBackInvoiceAsync(string invoiceId, bool explicitRetry, CancellationToken ct)
    {
        var lines = await context.XeroLedgerLines
            .Where(line => line.XeroInvoiceId == invoiceId)
            .ToListAsync(ct);
        if (lines.Count == 0)
            return new XeroWriteBackOutcome(false, "No stored ledger lines for this invoice.");

        var status = lines[0].InvoiceStatus;
        if (!AwaitingApprovalStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
        {
            // Approved/paid outside JPMS: the allocation stays portal-side, nothing to do —
            // a retry that races an out-of-band approval is a success, not an error.
            if (status.Equals("AUTHORISED", StringComparison.OrdinalIgnoreCase)
                || status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
                return new XeroWriteBackOutcome(true, null);
            return new XeroWriteBackOutcome(!explicitRetry, explicitRetry
                ? $"The invoice is {status} in Xero — only draft or submitted bills are approved from here."
                : null);
        }

        if (lines.Any(line => line.AllocationStatus != (int)XeroAllocationStatus.Allocated))
        {
            // Not ready (other lines still queued, or bucketed/ignored). Silent for the
            // automatic path — this is the normal state between allocating line 1 and line N.
            return new XeroWriteBackOutcome(false, explicitRetry
                ? "Every line of the bill must be allocated to a project and cost centre before it can be approved in Xero."
                : null);
        }

        // Resolve each line's project → Xero Sites option; explicit mapping, no guessing.
        var projectIds = lines.Select(line => line.ProjectId!).Distinct().ToList();
        var projects = await context.Projects
            .Where(project => projectIds.Contains(project.ProjectId))
            .ToDictionaryAsync(project => project.ProjectId, ct);
        var unmapped = projectIds
            .Where(id => !projects.TryGetValue(id, out var project) || string.IsNullOrWhiteSpace(project.XeroSiteName))
            .Select(id => projects.TryGetValue(id, out var project) ? project.Name : id)
            .ToList();
        if (unmapped.Count > 0)
            return await StampFailureAsync(lines,
                "No Xero site is mapped for " + string.Join(", ", unmapped)
                + " — set \"Xero site (tracking option)\" in the project's details, then retry.", ct);

        // Cost-centre splits: whole-line allocations carry their code on the line itself;
        // split lines carry rows in XeroCostSplits instead.
        var lineIds = lines.Select(line => line.XeroLedgerLineId).ToList();
        var splitsByLine = (await context.XeroCostSplits
                .Where(split => lineIds.Contains(split.XeroLedgerLineId))
                .ToListAsync(ct))
            .GroupBy(split => split.XeroLedgerLineId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var instructions = new List<XeroApprovalLineInstruction>();
        foreach (var line in lines)
        {
            var site = projects[line.ProjectId!].XeroSiteName!;
            var splits = splitsByLine.TryGetValue(line.XeroLedgerLineId, out var rows)
                ? rows.Select(row => new XeroCostSplit(row.CostCenterCode, row.Net)).ToList()
                : line.CostCenterCode is not null
                    ? new List<XeroCostSplit> { new(line.CostCenterCode, line.Net) }
                    : null;
            if (splits is null or { Count: 0 })
                return await StampFailureAsync(lines,
                    $"Line \"{line.Description}\" is allocated but carries no cost centre — re-allocate it.", ct);
            instructions.Add(new XeroApprovalLineInstruction(line.XeroLineItemId, site, splits));
        }

        var result = await xero.ApproveInvoiceAsync(
            new XeroApprovalRequest(invoiceId, lines[0].Type == "ACCPAYCREDIT", instructions), ct);

        var now = DateTimeOffset.UtcNow;
        if (result.Succeeded)
        {
            foreach (var line in lines)
            {
                line.WriteBackStatus = (int)XeroWriteBackStatus.Approved;
                line.WriteBackError = null;
                line.WriteBackAtUtc = now;
                // Reflect the approval immediately; the next sync re-confirms from Xero.
                line.InvoiceStatus = result.FreshStatus ?? "AUTHORISED";
            }
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Xero invoice {InvoiceId} approved with tracking for {LineCount} lines{Already}.",
                invoiceId, lines.Count, result.AlreadyApproved ? " (already approved in Xero)" : "");
            return new XeroWriteBackOutcome(true, null);
        }

        return await StampFailureAsync(lines, result.Error ?? "Xero rejected the update.", ct);
    }

    private async Task<XeroWriteBackOutcome> StampFailureAsync(
        List<XeroLedgerLineEntity> lines, string error, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var line in lines)
        {
            line.WriteBackStatus = (int)XeroWriteBackStatus.Failed;
            line.WriteBackError = error.Length <= 1024 ? error : error[..1024];
            line.WriteBackAtUtc = now;
        }
        await context.SaveChangesAsync(ct);
        logger.LogWarning("Xero write-back failed for invoice {InvoiceId}: {Error}", lines[0].XeroInvoiceId, error);
        return new XeroWriteBackOutcome(false, error);
    }
}
