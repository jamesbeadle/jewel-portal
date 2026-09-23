using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Forms.Links;

/// <summary>
/// A pack's clock (FormLinkLifetimes): fourteen days from sending, kept alive for at least seven more
/// whenever the person opens it or sends one of its forms — a pack that dies while somebody is half
/// way through is worse than no pack — and complete when its last form is in. Each form's own link
/// dies with the pack, so its state on the office's screen always agrees with the pack's.
/// </summary>
internal static class FormPackLife
{
    public static void KeepAlive(FormPackEntity pack, IEnumerable<FormInviteEntity> invites, DateTimeOffset now)
    {
        pack.OpenedAt ??= now;
        var lastSent = pack.LastChasedAt ?? pack.SentAt;
        pack.ExpiresAt = FormLinkLifetimes.PackKeptAliveAt(pack.ExpiresAt, lastSent, now);
        ExpireOutstandingWithThePack(pack, invites);
    }

    public static void RecordFormSent(FormPackEntity pack, IReadOnlyList<FormInviteEntity> invites, DateTimeOffset now)
    {
        KeepAlive(pack, invites, now);
        var isComplete = invites.All(invite => invite.UsedAt is not null);
        if (isComplete) pack.CompletedAt ??= now;
    }

    /// <summary>Sending again: a new link, a fresh fourteen days; what is already done stays done.</summary>
    public static void Resend(FormPackEntity pack, IEnumerable<FormInviteEntity> invites, string tokenHash, DateTimeOffset now)
    {
        pack.TokenHash = tokenHash;
        pack.ExpiresAt = now + FormLinkLifetimes.PackOnSending;
        ExpireOutstandingWithThePack(pack, invites);
    }

    private static void ExpireOutstandingWithThePack(FormPackEntity pack, IEnumerable<FormInviteEntity> invites)
    {
        var outstanding = invites.Where(invite => invite.UsedAt is null && invite.ExpiresAt < pack.ExpiresAt);
        foreach (var invite in outstanding) invite.ExpiresAt = pack.ExpiresAt;
    }
}
