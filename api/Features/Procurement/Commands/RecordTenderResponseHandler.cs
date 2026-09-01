using Jewel.JPMS.Contracts.Procurement;

namespace Jewel.JPMS.Api.Features.Procurement.Commands;

// The Control Centre's "File Bid Package Tender" follow-through: the email's tag filed it under the
// package (that already happened when the action's target was staged); this marks the sender's
// recipient row Responded so the Tender list says their tender is back before anyone extracts it.
// Matching mirrors the extraction handler: exact directory email, else a unique non-freemail
// domain. No match, or a recipient already Responded/Declined/Won, changes nothing — filing an
// email must never fail an Apply, and declining or winning is never undone by more mail arriving.
public sealed class RecordTenderResponseHandler
    : ICommandHandler<RecordTenderResponse, IReadOnlyList<BidPackageRecipient>>
{
    private static readonly HashSet<string> FreemailDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "gmail.com", "googlemail.com", "outlook.com", "hotmail.com", "hotmail.co.uk", "live.com",
        "live.co.uk", "yahoo.com", "yahoo.co.uk", "icloud.com", "me.com", "aol.com", "btinternet.com",
        "btopenworld.com", "sky.com", "talktalk.net", "virginmedia.com", "mail.com", "protonmail.com"
    };

    private readonly JpmsContext context;

    public RecordTenderResponseHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<BidPackageRecipient>> HandleAsync(RecordTenderResponse command, CancellationToken cancellationToken)
    {
        var package = await context.BidPackages.FindAsync(new object[] { command.BidPackageId }, cancellationToken);
        if (package is null) throw new InvalidOperationException($"Bid package {command.BidPackageId} not found.");

        var rows = await (
            from recipient in context.BidPackageRecipients
            where recipient.BidPackageId == command.BidPackageId
            join sub in context.Subcontractors on recipient.SubcontractorId equals sub.SubcontractorId
            select new { recipient, sub.ContactEmail })
            .ToListAsync(cancellationToken);

        var matched = MatchBySender(rows.Select(r => (r.recipient, r.ContactEmail)).ToList(), command.SenderEmail);
        if (matched is not null && matched.Status == (int)BidPackageRecipientStatus.Invited)
        {
            matched.Status = (int)BidPackageRecipientStatus.Responded;
            matched.RespondedAt ??= DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }

        var all = await context.BidPackageRecipients
            .Where(r => r.BidPackageId == command.BidPackageId)
            .OrderBy(r => r.InvitedAt)
            .ToListAsync(cancellationToken);
        return all.Select(e => e.ToModel()).ToList().AsReadOnly();
    }

    private static Data.Entities.BidPackageRecipientEntity? MatchBySender(
        IReadOnlyList<(Data.Entities.BidPackageRecipientEntity Recipient, string? ContactEmail)> rows,
        string senderEmail)
    {
        if (string.IsNullOrWhiteSpace(senderEmail)) return null;

        var exact = rows.FirstOrDefault(r =>
            !string.IsNullOrWhiteSpace(r.ContactEmail)
            && string.Equals(r.ContactEmail!.Trim(), senderEmail.Trim(), StringComparison.OrdinalIgnoreCase));
        if (exact.Recipient is not null) return exact.Recipient;

        var at = senderEmail.LastIndexOf('@');
        var domain = at > 0 ? senderEmail[(at + 1)..].Trim() : "";
        if (domain.Length == 0 || FreemailDomains.Contains(domain)) return null;

        var byDomain = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.ContactEmail) && r.ContactEmail!.Contains('@')
                && string.Equals(r.ContactEmail[(r.ContactEmail.LastIndexOf('@') + 1)..].Trim(), domain,
                    StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Recipient)
            .DistinctBy(r => r.SubcontractorId)
            .ToList();
        return byDomain.Count == 1 ? byDomain[0] : null;
    }
}
