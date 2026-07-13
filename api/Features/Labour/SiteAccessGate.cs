using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>
/// Authenticates the anonymous site capture surface. The QR token is the only credential:
/// it resolves to exactly one project while active, and rotation deactivates it. Every
/// site-labour endpoint must go through this gate — there is no session on this surface.
/// </summary>
public sealed class SiteAccessGate
{
    private readonly JpmsContext context;
    public SiteAccessGate(JpmsContext context) { this.context = context; }

    public async Task<SiteAccessTokenEntity?> ResolveAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 64) return null;
        return await context.SiteAccessTokens
            .FirstOrDefaultAsync(access => access.Token == token && access.IsActive, cancellationToken);
    }
}

/// <summary>
/// Working-day arithmetic for site capture. Sites run on UK local time; the working date is
/// the UK-local calendar date, stored as midnight UTC so day-uniqueness comparisons are exact.
/// </summary>
public static class SiteClock
{
    private static readonly TimeZoneInfo UkTime = ResolveUkTimeZone();

    private static TimeZoneInfo ResolveUkTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/London"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"); }
    }

    public static DateTimeOffset WorkDateOf(DateTimeOffset moment) =>
        new(TimeZoneInfo.ConvertTime(moment, UkTime).Date, TimeSpan.Zero);

    public static DateTimeOffset Today() => WorkDateOf(DateTimeOffset.UtcNow);
}
