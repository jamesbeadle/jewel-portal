namespace Jewel.JPMS.Api.Features.Procurement;

/// <summary>
/// Which quote a caller may put a price on. The role gate answers "may this kind of user price
/// work?"; this answers "is this price theirs to give?".
///
/// Role.Subcontractor is admitted so a tenderer can answer their own invitation — but it is
/// admitted for ANY bid package id and ANY quote id, and SubmitQuoteForBidPackage names the
/// subcontractor in its BODY, so without this a signed-in tenderer could revise a competitor's
/// quote or submit one in a competitor's name (permission check, 2026-09-19). A caller who is not
/// an external tenderer places work on every package, and their role is the whole answer.
/// </summary>
internal static class QuoteScope
{
    public static async Task<bool> MayPriceBidPackageAsync(
        JpmsContext context, SignedInUser user, string bidPackageId, string quotedFor,
        CancellationToken cancellationToken)
    {
        var tenderer = SubcontractorScope.OwnSubcontractorId(user);
        if (tenderer is null) return true;
        if (quotedFor != tenderer) return false;

        return await context.BidPackageRecipients
            .AsNoTracking()
            .AnyAsync(recipient => recipient.BidPackageId == bidPackageId
                && recipient.SubcontractorId == tenderer, cancellationToken);
    }

    public static async Task<bool> MayReviseQuoteAsync(
        JpmsContext context, SignedInUser user, string quoteId, CancellationToken cancellationToken)
    {
        var tenderer = SubcontractorScope.OwnSubcontractorId(user);
        if (tenderer is null) return true;

        return await context.Quotes
            .AsNoTracking()
            .AnyAsync(quote => quote.QuoteId == quoteId && quote.SubcontractorId == tenderer,
                cancellationToken);
    }
}
