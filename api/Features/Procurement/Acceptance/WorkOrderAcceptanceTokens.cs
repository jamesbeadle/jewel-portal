using Jewel.JPMS.Api.Auth;
using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Procurement.Acceptance;

/// <summary>
/// One acceptance token per order, minted the first time the purchase order is emailed and kept
/// for every later send (Nigel, 2026-09-23): an older email in the supplier's inbox still works,
/// and the token is dead only because the order itself has closed — the public service refuses
/// Complete, Cancelled and Rejected orders whatever token they carry. The caller saves.
/// </summary>
public static class WorkOrderAcceptanceTokens
{
    public static string MintIfMissing(WorkOrderEntity order, DateTimeOffset now)
    {
        if (!string.IsNullOrWhiteSpace(order.AcceptanceToken)) return order.AcceptanceToken;
        order.AcceptanceToken = AuthTokens.NewSecret();
        order.AcceptanceTokenIssuedAt = now;
        return order.AcceptanceToken;
    }
}
