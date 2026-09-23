using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Procurement.Acceptance;

/// <summary>
/// The one rule for electronically accepting a work order, whichever door it comes through — the
/// subcontractor's portal login (AcceptMyWorkOrderEndpoint) or the acceptance link in the
/// purchase-order email (WorkOrderAcceptanceService). Only an issued order can be accepted, and
/// an order already accepted stays as it is: a double-click or a stale second tab never surfaces
/// an error for an outcome that already holds. The caller saves.
/// </summary>
public static class WorkOrderAcceptance
{
    public const string OnlyIssuedOrdersRefusal = "Only issued work orders can be accepted.";

    public static bool IsIssued(WorkOrderEntity order) => order.Status == (int)WorkOrderStatus.Released;

    public static bool IsAccepted(WorkOrderEntity order) => order.AcceptedAt is not null;

    /// <summary>Stamps the acceptance; false when the order is not an issued one. An order
    /// already accepted answers true without changing who accepted it or when.</summary>
    public static bool TryStamp(WorkOrderEntity order, string accepterName, string accepterEmail, DateTimeOffset now)
    {
        if (IsAccepted(order)) return true;
        if (!IsIssued(order)) return false;
        order.AcceptedAt = now;
        order.AcceptedByName = accepterName;
        order.AcceptedByEmail = accepterEmail;
        return true;
    }
}
