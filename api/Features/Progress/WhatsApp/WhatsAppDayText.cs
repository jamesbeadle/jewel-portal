using System.Globalization;
using System.Text;

namespace Jewel.JPMS.Api.Features.Progress.WhatsApp;

/// <summary>
/// What a day's update says. The title names the day; the description is the day's messages
/// verbatim, one per line with its time and sender — raw site shorthand kept as it is, because the
/// translation into report language happens at drafting and the original note stays on the record.
/// </summary>
public static class WhatsAppDayText
{
    private static readonly CultureInfo British = CultureInfo.GetCultureInfo("en-GB");

    public const string TitlePrefix = "Site notes — ";

    public static string Title(DateOnly day) => TitlePrefix + day.ToString("dddd d MMMM yyyy", British);

    public static string Description(IReadOnlyList<WhatsAppMessage> messages)
    {
        var text = new StringBuilder();
        foreach (var message in messages.Where(message => message.Text.Length > 0))
        {
            if (text.Length > 0) text.Append('\n');
            text.Append(message.SentAt.ToString("HH:mm", British))
                .Append(' ')
                .Append(message.Sender)
                .Append(": ")
                .Append(message.Text);
        }
        return text.ToString();
    }
}
