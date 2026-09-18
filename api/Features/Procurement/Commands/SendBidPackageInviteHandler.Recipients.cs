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

    /// <summary>The command carries addresses now, so this only puts them in the shape Graph wants.
    /// The "@" check stays because an address can still arrive from the connector, where nothing has
    /// been through a recipient field.</summary>
    private static List<MailboxDraftRecipient> ParseRecipients(IReadOnlyList<string>? addresses) =>
        (addresses ?? Array.Empty<string>())
            .Select(address => address.Trim())
            .Where(address => address.Contains('@', StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(address => new MailboxDraftRecipient(address))
            .ToList();
}
