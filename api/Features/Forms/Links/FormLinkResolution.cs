using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Forms.Links;

/// <summary>A link looked up: the invite (and its pack) it opens, or why it cannot open anything.</summary>
public sealed record ResolvedLink(FormInviteEntity? Invite, FormPackEntity? Pack, FormLinkProblem? Problem)
{
    public static readonly ResolvedLink NotValid = new(null, null, FormLinkProblem.NotValid);

    public bool IsOpen => Problem is null;
}

/// <summary>
/// Looking a link up without telling a stranger which of the failures it was (lib/invites.js
/// resolveToken): unknown, cancelled, replaced and mistyped all read "not valid"; only a genuinely
/// spent or expired link says so. A link that was opened while it was alive is still honoured when
/// the form it opened is sent, so a man who started the form is never turned away at the end of it.
/// A form of a pack sent again on its own opens by its own link and still counts towards the pack.
/// </summary>
internal static class FormLinkResolution
{
    public static async Task<ResolvedLink> ForInviteAsync(
        JpmsContext context, string? token, string formSlug, CancellationToken cancellationToken)
    {
        if (!FormTokens.IsWellFormed(token)) return ResolvedLink.NotValid;
        var hash = FormTokens.Hash(token!);
        var invite = await context.FormInvites.FirstOrDefaultAsync(row => row.TokenHash == hash, cancellationToken);
        var isThisForm = invite is not null && invite.FormSlug == formSlug;
        if (!isThisForm) return ResolvedLink.NotValid;
        var pack = await context.FormPacks.FirstOrDefaultAsync(row => row.FormPackId == invite!.FormPackId, cancellationToken);
        return Judged(invite!, pack, invite!.ExpiresAt);
    }

    public static async Task<ResolvedLink> ForPackAsync(JpmsContext context, string? token, CancellationToken cancellationToken)
    {
        if (!FormTokens.IsWellFormed(token)) return ResolvedLink.NotValid;
        var hash = FormTokens.Hash(token!);
        var pack = await context.FormPacks.FirstOrDefaultAsync(row => row.TokenHash == hash, cancellationToken);
        if (pack is null || pack.CancelledAt is not null) return ResolvedLink.NotValid;
        var hasExpired = pack.ExpiresAt < DateTimeOffset.UtcNow;
        return new ResolvedLink(null, pack, hasExpired ? FormLinkProblem.Expired : null);
    }

    public static async Task<ResolvedLink> ForPackFormAsync(
        JpmsContext context, string? token, string formSlug, CancellationToken cancellationToken)
    {
        var packLink = await ForPackAsync(context, token, cancellationToken);
        var pack = packLink.Pack;
        if (pack is null) return ResolvedLink.NotValid;
        var invite = await context.FormInvites.FirstOrDefaultAsync(
            row => row.FormPackId == pack.FormPackId && row.FormSlug == formSlug && row.CancelledAt == null, cancellationToken);
        return invite is null ? ResolvedLink.NotValid : Judged(invite, pack, pack.ExpiresAt);
    }

    public static bool IsHonouredOnSending(ResolvedLink link)
    {
        var wasOpenedInTime = link.Problem == FormLinkProblem.Expired && link.Invite?.OpenedAt is not null;
        return link.IsOpen || wasOpenedInTime;
    }

    private static ResolvedLink Judged(FormInviteEntity invite, FormPackEntity? pack, DateTimeOffset expiresAt)
    {
        if (invite.CancelledAt is not null) return ResolvedLink.NotValid;
        if (invite.UsedAt is not null) return new ResolvedLink(invite, pack, FormLinkProblem.Used);
        var hasExpired = expiresAt < DateTimeOffset.UtcNow;
        return new ResolvedLink(invite, pack, hasExpired ? FormLinkProblem.Expired : null);
    }
}
