using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Procurement;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

/// <summary>
/// Edits a work order wholesale — supplier, title, scope and priced lines —
/// recomputing the order's value as the sum of its lines. Orders raised directly in
/// JPMS (no bid package, no variation, no seed source) are editable by everyone the
/// authorisation admits, as they always were. Orders a source flow owns — a tender
/// award, a variation instruction, a Buildertrend seed — are editable only when the
/// endpoint stamped EditorMayEditAnyOrder (MD / FD / Admin): correcting a live order
/// against what was actually agreed (2026-08-21, the accountant's add-a-line flow)
/// is a directors' decision, and the edit deliberately does NOT write back to the
/// source record — the order becomes the truth of what was instructed. Rejected and
/// Cancelled orders are terminal records and never editable. Lines keep their ids
/// where the edit keeps them, so paid-to-date and invoice history stay attached; a
/// line can only be removed while nothing has been paid against it, and a kept line
/// can't be priced below what has already been paid. Saving never re-emails the
/// supplier — the updated PO is sent from the PO page by hand.
/// </summary>
public sealed class UpdateManualWorkOrderHandler
    : ICommandHandler<UpdateManualWorkOrder, WorkOrder>
{
    private readonly JpmsContext context;

    public UpdateManualWorkOrderHandler(JpmsContext context) { this.context = context; }

    public async Task<WorkOrder> HandleAsync(UpdateManualWorkOrder command, CancellationToken cancellationToken)
    {
        var entity = await context.WorkOrders.FindAsync(new object[] { command.WorkOrderId }, cancellationToken);
        if (entity is null || !string.Equals(entity.ProjectId, command.ProjectId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("This work order does not belong to this project.");

        // Terminal records first: they stand as the written history of a decision, whoever asks.
        if (entity.Status == (int)WorkOrderStatus.Rejected)
            throw new InvalidOperationException("This work order was rejected — it can't be edited. Raise a fresh order instead.");
        if (entity.Status == (int)WorkOrderStatus.Cancelled)
            throw new InvalidOperationException("This work order was cancelled — it stands as the record of the voided purchase order and can't be edited.");

        // Orders a source flow owns are a directors' correction only (the endpoint stamps the
        // flag from the sign-in — MD / FD / Admin). The messages keep pointing everyone else at
        // the flow that owns the order.
        if (!command.EditorMayEditAnyOrder)
        {
            if (entity.BidPackageId is not null)
                throw new InvalidOperationException("This order was awarded from a tender — its value and lines are owned by the bid package. Only the MD, FD or an administrator can correct it here.");
            if (entity.VariationOrderId is not null)
                throw new InvalidOperationException("This order instructs a variation — edit the variation order instead. Only the MD, FD or an administrator can correct the order itself here.");
            if (entity.SourceReference is not null)
                throw new InvalidOperationException("This order was seeded from the source system — only the MD, FD or an administrator can edit it here.");
        }

        var subcontractor = await context.Subcontractors.FindAsync(new object[] { command.SubcontractorId }, cancellationToken);
        if (subcontractor is null) throw new InvalidOperationException("Choose a subcontractor from the directory.");

        // Every line must land on a centre in the master list, so the edit can't scatter
        // committed value onto codes the Financials tab treats as legacy.
        var masterCodes = await context.CostCenters
            .Select(centre => centre.Code)
            .ToListAsync(cancellationToken);
        var masterSet = new HashSet<string>(masterCodes, StringComparer.OrdinalIgnoreCase);
        var unknown = command.Lines.Select(line => line.CostCode)
            .FirstOrDefault(code => !masterSet.Contains(code));
        if (unknown is not null)
            throw new InvalidOperationException($"Cost centre {unknown} is not in the cost-code master.");

        var existingLines = await context.WorkOrderLines
            .Where(line => line.WorkOrderId == entity.WorkOrderId)
            .ToListAsync(cancellationToken);
        var existingById = existingLines.ToDictionary(line => line.WorkOrderLineId, StringComparer.OrdinalIgnoreCase);

        var keptIds = new HashSet<string>(
            command.Lines.Where(line => line.WorkOrderLineId is not null).Select(line => line.WorkOrderLineId!),
            StringComparer.OrdinalIgnoreCase);
        var strayId = keptIds.FirstOrDefault(id => !existingById.ContainsKey(id));
        if (strayId is not null)
            throw new InvalidOperationException($"Line {strayId} does not belong to this work order.");

        // Removals: only lines nothing has been paid against — a paid line anchors the
        // Financials tab's paid figures and must stay (re-code it instead if misplaced).
        foreach (var removed in existingLines.Where(line => !keptIds.Contains(line.WorkOrderLineId)))
        {
            if (removed.PaidToDate != 0m)
                throw new InvalidOperationException(
                    $"\"{removed.Title}\" has {removed.PaidToDate:0.00} paid against it and can't be removed.");
            context.WorkOrderLines.Remove(removed);
        }

        var sortOrder = 0;
        foreach (var line in command.Lines)
        {
            if (line.WorkOrderLineId is not null)
            {
                var existing = existingById[line.WorkOrderLineId];
                if (existing.PaidToDate != 0m && Math.Sign(line.Amount) != Math.Sign(existing.PaidToDate))
                    throw new InvalidOperationException(
                        $"\"{existing.Title}\" has {existing.PaidToDate:0.00} paid against it — the amount must keep its sign.");
                if (Math.Abs(line.Amount) < Math.Abs(existing.PaidToDate))
                    throw new InvalidOperationException(
                        $"\"{existing.Title}\" has {existing.PaidToDate:0.00} paid against it — the amount can't drop below that.");

                existing.Title = line.Title.Length > 256 ? line.Title[..256] : line.Title;
                existing.Description = line.Description.Length > 1024 ? line.Description[..1024] : line.Description;
                existing.CostCode = line.CostCode;
                existing.Quantity = 1m;
                existing.Unit = "item";
                existing.UnitCost = line.Amount;
                existing.LineTotal = line.Amount;
                existing.SortOrder = sortOrder++;
            }
            else
            {
                context.WorkOrderLines.Add(new WorkOrderLineEntity
                {
                    WorkOrderLineId = ProcurementIdentifierFactory.NextWorkOrderLineId(),
                    WorkOrderId = entity.WorkOrderId,
                    Title = line.Title.Length > 256 ? line.Title[..256] : line.Title,
                    Description = line.Description.Length > 1024 ? line.Description[..1024] : line.Description,
                    CostType = "Subcontractor",
                    CostCode = line.CostCode,
                    Quantity = 1m,
                    Unit = "item",
                    UnitCost = line.Amount,
                    LineTotal = line.Amount,
                    PaidToDate = 0m,
                    SortOrder = sortOrder++
                });
            }
        }

        entity.SubcontractorId = command.SubcontractorId;
        entity.Title = command.Title.Length > 256 ? command.Title[..256] : command.Title;
        entity.Scope = command.Scope.Length > 4000 ? command.Scope[..4000] : command.Scope;
        entity.Value = command.Lines.Sum(line => line.Amount);
        entity.ProgrammeStart = command.ProgrammeStart;
        entity.ScheduledCompletion = command.TargetCompletion;
        entity.ProgrammeNotes = command.ProgrammeNotes.Length > 2000 ? command.ProgrammeNotes[..2000] : command.ProgrammeNotes;
        // Percent never survives without the flag — an untick clears it rather than leaving
        // a stale figure that would print the moment the box is ticked again.
        entity.DepositRequired = command.DepositRequired;
        entity.DepositPercent = command.DepositRequired ? command.DepositPercent : null;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}
