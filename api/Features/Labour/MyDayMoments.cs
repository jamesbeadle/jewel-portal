namespace Jewel.JPMS.Api.Features.Labour;

/// <summary>A sign-in or sign-out time as the worker gives it: the moment now unless they adjusted
/// it, and then only a time on today's working day that has already passed — a forgotten sign-in
/// is earlier than now, never later.</summary>
public static class MyDayMoments
{
    private static readonly TimeSpan ClockAllowance = TimeSpan.FromMinutes(5);

    public static DateTimeOffset Resolve(DateTimeOffset? given, DateTimeOffset today, string what)
    {
        var now = DateTimeOffset.UtcNow;
        if (given is not { } moment) return now;
        var isOnToday = SiteClock.WorkDateOf(moment) == today;
        if (!isOnToday) throw new InvalidOperationException($"The {what} time must be a time today.");
        var isInTheFuture = moment > now + ClockAllowance;
        if (isInTheFuture) throw new InvalidOperationException($"The {what} time cannot be later than now.");
        return moment;
    }
}
