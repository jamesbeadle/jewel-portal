namespace Jewel.JPMS.Api.Features.Progress.WhatsApp;

/// <summary>The week sorted into days, before any file is opened: this project's messages by day,
/// the messages for a person to look at, and the counts of what was set aside.</summary>
public sealed record WhatsAppWeekPlan(
    WhatsAppWeek Week,
    IReadOnlyList<WhatsAppPlannedDay> Days,
    IReadOnlyList<WhatsAppMessage> Unattributed,
    int OtherSiteMessageCount,
    IReadOnlyList<string> OtherSiteSenders,
    int MessagesOutsideWeek);

/// <summary>One day's messages for this project, in the order they were sent.</summary>
public sealed record WhatsAppPlannedDay(DateOnly Date, IReadOnlyList<WhatsAppMessage> Messages)
{
    public IReadOnlyList<string> MediaFileNames =>
        Messages.Where(message => message.HasMedia).Select(message => message.MediaFileName!).ToList();
}

/// <summary>
/// Cuts the export to the week and sorts each message by its sender: this project's messages
/// become the day they were sent on, another project's are counted and set aside, and a sender
/// on no list goes to the review list rather than being guessed at. Pure.
/// </summary>
public static class WhatsAppWeekPlanner
{
    public static WhatsAppWeekPlan Plan(IReadOnlyList<WhatsAppMessage> messages, WhatsAppWeek week, WhatsAppSenderAttribution senders)
    {
        var inWeek = messages.Where(message => week.Contains(message.Day)).ToList();
        var bySite = inWeek.ToLookup(message => senders.SiteOf(message.Sender));

        var days = bySite[WhatsAppSenderSite.ThisProject]
            .GroupBy(message => message.Day)
            .OrderBy(group => group.Key)
            .Select(group => new WhatsAppPlannedDay(group.Key, group.OrderBy(message => message.SentAt).ToList()))
            .ToList();

        var otherSite = bySite[WhatsAppSenderSite.OtherProject].ToList();
        return new WhatsAppWeekPlan(
            week,
            days,
            bySite[WhatsAppSenderSite.Unknown].OrderBy(message => message.SentAt).ToList(),
            otherSite.Count,
            otherSite.Select(message => message.Sender).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name).ToList(),
            messages.Count - inWeek.Count);
    }
}
