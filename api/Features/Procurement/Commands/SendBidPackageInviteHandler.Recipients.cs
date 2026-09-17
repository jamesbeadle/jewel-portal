using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

public sealed partial class SendBidPackageInviteHandler
{
    /// <summary>The composer draft has served its purpose once the invite has gone; the sent copy,
    /// tagged to the package, is the record now — readable under Tender responses & related
    /// emails.</summary>
    private async Task ClearComposerDraftAsync(BidPackageEntity package, CancellationToken cancellationToken)
    {
        package.InviteDraftSubject = null;
        package.InviteDraftBody = null;
        package.InviteDraftTo = null;
        package.InviteDraftCc = null;
        package.InviteDraftBcc = null;
        package.InviteDraftSavedAt = null;
        await context.SaveChangesAsync(cancellationToken);
    }

    private static List<MailboxDraftRecipient> ParseRecipients(string? raw) =>
        (raw ?? "")
            .Split(new[] { ';', ',' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(address => address.Contains('@', StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(address => new MailboxDraftRecipient(address))
            .ToList();
}
