using System.Text.Json;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Contracts.Boq;
using Jewel.JPMS.Contracts.Drawings;
using Jewel.JPMS.Contracts.Variations;
using Jewel.JPMS.Features.CostCenters;
using Jewel.JPMS.Features.Procurement;
using Jewel.JPMS.Features.Triage;
using Jewel.JPMS.Features.Triage.Panels;

namespace Jewel.JPMS.Pages;

public partial class ProjectBidPackageInviteDetail
{
    // ---- Award summary & work-order email to the winner ----

    // The order this package's award raised (latest, if re-awarded). Null until orders load or
    // when the package has never been awarded.
    private ProjectWorkOrderDetail? AwardedOrder => projectOrders
        .Where(detail => string.Equals(detail.Order.BidPackageId, BidPackageId, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(detail => detail.Order.AwardedAt)
        .FirstOrDefault();

    private bool showWoEmailModal;
    private string woEmailSubject = "";
    private string woEmailBody = "";
    private string? woEmailNote;
    private string? woEmailLink;

    private void OpenWorkOrderEmailModal()
    {
        if (busy || package is null || AwardedOrder is not { } awarded) return;
        woEmailSubject = $"Work order WO-{awarded.Order.Number:0000} — {awarded.Order.Title} ({package.Reference})";
        woEmailBody = DefaultWorkOrderEmailBody(awarded);
        woEmailNote = null;
        showWoEmailModal = true;
    }

    private void CloseWorkOrderEmailModal() => showWoEmailModal = false;

    // The pre-filled order email: award confirmation, the priced lines (or the order total and scope
    // for legacy orders without lines), and the pre-start paperwork the tender invite asked about.
    private string DefaultWorkOrderEmailBody(ProjectWorkOrderDetail awarded)
    {
        var order = awarded.Order;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"<p>Hello {awarded.SubcontractorName},</p>");
        sb.AppendLine($"<p>Following your tender for the <strong>{package!.Title}</strong> package (ref {package.Reference}), we are pleased to confirm the award and attach our work order <strong>WO-{order.Number:0000}</strong> below.</p>");
        sb.AppendLine(awarded.Lines.Count > 0
            ? WorkOrderPoEmail.LinesTable(OrderedLines(awarded), order.Value)
            : OrderValueAndScope(order));
        if (order.ScheduledCompletion is { } completion)
            sb.AppendLine($"<p><strong>Scheduled completion:</strong> {completion.LocalDateTime:d MMM yyyy}</p>");
        sb.AppendLine("<p>Please reply to confirm receipt and acceptance of this order, quoting the reference. Before starting on site, please provide your RAMS documentation and current insurance certificates as set out in the tender invitation.</p>");
        sb.AppendLine("<p>Kind regards,<br/>Jewel Bespoke Build</p>");
        return sb.ToString();
    }

    private static IReadOnlyList<WorkOrderPoEmail.Line> OrderedLines(ProjectWorkOrderDetail awarded) =>
        awarded.Lines.OrderBy(line => line.SortOrder).Select(WorkOrderPoEmail.ToLine).ToList();

    private static string OrderValueAndScope(WorkOrder order)
    {
        var value = $"<p><strong>Order value:</strong> {order.Value:£#,##0.00}</p>";
        if (string.IsNullOrWhiteSpace(order.Scope)) return value;
        return value + Environment.NewLine + $"<p><strong>Scope:</strong> {order.Scope}</p>";
    }

    private async Task EmailWorkOrderToWinner(bool saveAsDraftOnly)
    {
        if (busy || !CanEdit || AwardedOrder is not { } awarded) return;
        error = null;
        try
        {
            busy = true;
            var outcome = await Commands.SendAsync(
                new SendWorkOrderPoEmail(awarded.Order.WorkOrderId, woEmailSubject.Trim(), woEmailBody, saveAsDraftOnly),
                CancellationToken.None);
            showWoEmailModal = false;
            woEmailLink = outcome.WebLink;
            woEmailNote = WorkOrderEmailNote(outcome);
        }
        catch (CommandFailedException ex) { error = $"Couldn't email the work order: {ex.Message}"; }
        catch { error = "Couldn't email the work order. Check the supplier has an email address in the directory and the mailbox connection, then try again."; }
        finally { busy = false; }
    }

    private string WorkOrderEmailNote(WorkOrderPoEmailOutcome outcome)
    {
        if (outcome.Sent)
            return $"Sent to {outcome.RecipientEmail} with the purchase order attached, tagged {package?.Reference}.";
        if (outcome.FailureNote is { } failure) return failure;
        return $"Draft created in the shared mailbox to {outcome.RecipientEmail}, tagged {package?.Reference}. "
            + "Review and send it from the mailbox's Drafts folder.";
    }

    // ---- Award: winning quote → work order (the purchase-order record) ----

    private async Task AwardTo(Quote quote)
    {
        if (busy || package is null || !CanEdit) return;
        error = null;
        try
        {
            busy = true;
            var sub = Subs.Find(quote.SubcontractorId);
            var workOrder = await Commands.SendAsync(
                new AwardBidPackage(
                    BidPackageId, ProjectId, quote.SubcontractorId, quote.Value,
                    $"{package.Title} ({package.Reference}) — as tender submission received {quote.ReceivedAt.LocalDateTime:d MMM yyyy}",
                    Auth.CurrentUser?.Email ?? "", quote.QuoteId), CancellationToken.None);
            awardNote = $"Awarded to {sub?.CompanyName ?? quote.SubcontractorId} at {quote.Value:£#,##0.00} — work order {workOrder.WorkOrderId[..8]}… raised as the purchase order.";
            await LoadAsync();
        }
        catch { error = "Couldn't award the package. Please try again."; }
        finally { busy = false; }
    }

}
