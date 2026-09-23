using Jewel.JPMS.Contracts.Hs;

namespace Jewel.JPMS.Api.Features.Hs;

/// <summary>
/// The record-level rule on an update, asked by the endpoint and by the connector alike after the
/// role gate (the permission check's shape): a corrective action is closed only by the H&amp;S
/// officer or a director (HsActionRoles.AllowedToClose) — on her next visit or on a photograph —
/// never by the site manager who owns it. Any other change, and any other kind of record, passes.
/// </summary>
public static class HsRecordCloseScope
{
    public const string Refusal = "A corrective action is closed by the H&S officer, on her next visit or on a photograph of the work done — not by its owner.";

    public static async Task<bool> AllowsAsync(JpmsContext context, SignedInUser user, UpdateHsRecord command, CancellationToken cancellationToken)
    {
        var isClosing = command.Status == HsStatus.Closed;
        if (!isClosing) return true;
        var kind = await context.HsRecords.AsNoTracking()
            .Where(row => row.HsRecordId == command.HsRecordId)
            .Select(row => (int?)row.Kind)
            .FirstOrDefaultAsync(cancellationToken);
        var isACorrectiveAction = kind == (int)HsRecordKind.CorrectiveAction;
        if (!isACorrectiveAction) return true;
        return HsActionRoles.MayClose(user.Roles);
    }
}
