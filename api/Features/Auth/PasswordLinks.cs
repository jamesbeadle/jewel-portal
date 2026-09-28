namespace Jewel.JPMS.Api.Features.Auth;

/// <summary>
/// The invite and reset links still waiting in someone's inbox for one address. Whoever acts on a
/// password last has the only word: a reset voids the links issued before it, and a password being
/// set voids every link that could replace it.
/// </summary>
public static class PasswordLinks
{
    /// <summary>Marks every unused, unexpired link for the address as used. The caller saves.</summary>
    public static async Task VoidEveryLiveLinkAsync(
        JpmsContext context, string email, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var liveLinks = await context.PasswordResetTokens
            .Where(row => row.Email == email && row.ConsumedAt == null && row.ExpiresAt > now)
            .ToListAsync(cancellationToken);
        foreach (var link in liveLinks) link.ConsumedAt = now;
    }
}
