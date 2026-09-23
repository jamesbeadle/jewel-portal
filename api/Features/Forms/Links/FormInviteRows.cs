using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Forms.Links;

/// <summary>Who a link goes to: the person, their own company and their email.</summary>
public sealed record FormLinkRecipient(string PersonName, string CompanyName, string Email);

/// <summary>Who sent it, as the office's own sign-in says.</summary>
public sealed record FormLinkSender(string Email, string Name);

/// <summary>A new invite row and the secret that opens it — the secret is emailed once and never stored.</summary>
public sealed record IssuedInvite(FormInviteEntity Invite, string Token);

/// <summary>
/// Creating a one-time link (lib/invites.js create): the row carries who it is for, who sent it and
/// when it dies; only the token's hash is kept. A pack's forms get rows of their own, reached
/// through the pack's link, so each keeps its own opened, used and expired state.
/// </summary>
internal static class FormInviteRows
{
    private const int LongestName = 256;
    private const int LongestReason = 512;

    public static IssuedInvite New(
        string formSlug, FormLinkRecipient recipient, FormLinkSender sender, DateTimeOffset now, DateTimeOffset expiresAt,
        string reason, string? formPackId)
    {
        var token = FormTokens.NewSecret();
        var invite = new FormInviteEntity
        {
            FormInviteId = FormIdentifierFactory.NextId(),
            FormPackId = formPackId,
            FormSlug = formSlug,
            PersonName = Clip(recipient.PersonName, LongestName),
            CompanyName = Clip(recipient.CompanyName, LongestName),
            Email = Clip(recipient.Email, LongestName),
            TokenHash = FormTokens.Hash(token),
            ExpiresAt = expiresAt,
            SentByEmail = Clip(sender.Email, LongestName),
            SentByName = Clip(sender.Name, LongestName),
            SentAt = now,
            Reason = Clip(reason, LongestReason)
        };
        return new IssuedInvite(invite, token);
    }

    public static bool IsAnEmailAddress(string address)
    {
        var trimmed = address.Trim();
        var at = trimmed.IndexOf('@');
        var dot = trimmed.LastIndexOf('.');
        var hasNoSpaces = !trimmed.Contains(' ');
        return hasNoSpaces && at > 0 && dot > at + 1 && dot < trimmed.Length - 1 && trimmed.Length < LongestName;
    }

    private static string Clip(string value, int longest)
    {
        var trimmed = value.Trim();
        return trimmed.Length > longest ? trimmed[..longest] : trimmed;
    }
}
