using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

/// <summary>
/// Approves a draft work order: mints the next sequential per-project number (numbers
/// are only ever minted at approval, so rejected or abandoned drafts never leave gaps)
/// and moves the order to WorkOrderStatus.Released — from which point the supplier can
/// see and accept it, and WO allocation, reconciliation packages and Xero links treat
/// it like any other order. (A draft already counts in the Financials tab's committed
/// figures; approval doesn't change the money, it issues the order.) AwardedAt /
/// AwardedByEmail record the approval, since that is the moment the order is actually
/// issued. Open to the same roles that may raise orders directly.
/// </summary>
public sealed record ApproveWorkOrder(
    string ProjectId,
    string WorkOrderId,
    string ApprovedByEmail) : ICommand<WorkOrder>;
