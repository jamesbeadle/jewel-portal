using System.Globalization;
using static Jewel.JPMS.Api.Features.Forms.Mail.FormEmailFrame;

namespace Jewel.JPMS.Api.Features.Forms.Mail;

/// <summary>
/// The emails that carry a link: one form to one named person (api/invite.js emailHtml, word for
/// word), and a new starter's pack — one link for every form they owe, sent again as a reminder
/// when the office chases.
/// </summary>
internal static class FormLinkEmails
{
    private const string NotExpecting = "If you were not expecting this, please tell us and do not use the link.";

    public static FormEmail ForOneForm(
        string personName, string email, string formTitle, string reason, string sentBy, string link, DateTimeOffset expiresAt)
    {
        var sender = sentBy.Length > 0 ? sentBy : "The office";
        var smallPrint = $"The link works until <b>{Encode(Day(expiresAt))}</b> and can only be used once. "
            + "If it has expired, reply to this email and we will send another. " + NotExpecting;
        var html = Wrap(Paragraph(Hello(personName)) + Paragraph(Encode(reason))
            + Paragraph($"{Encode(sender)} has sent you this link. It is just for you.")
            + Button(link, "Open the form") + SmallPrint(smallPrint));
        var text = $"Hello {FirstName(personName)}\n\n{reason}\n\n{sender} has sent you this link. It is just for you.\n\n{link}\n\n"
            + $"The link works until {Day(expiresAt)} and can only be used once. If it has expired, reply to this email and we will "
            + $"send another. {NotExpecting}\n\n{FootLine}";
        return new FormEmail(new[] { email }, $"{formTitle} - {JewelBespokeBuild.LegalName}", html, text);
    }

    public static FormEmail ForPack(
        string personName, string email, IReadOnlyList<string> formTitles, string sentBy, string link,
        DateTimeOffset expiresAt, bool isReminder)
    {
        var opening = isReminder
            ? $"A reminder: we still need {HowMany(formTitles.Count)} from you before your first day. What you have already sent is safe."
            : $"Welcome to {JewelBespokeBuild.ShortName}. Before your first day we need {HowMany(formTitles.Count)} from you. "
                + "Each one takes a few minutes and works on your phone.";
        var list = string.Concat(formTitles.Select(title => $"<li>{Encode(title)}</li>"));
        var smallPrint = $"The link lasts until <b>{Encode(Day(expiresAt))}</b>, and a little longer each time you use it. You do not "
            + "have to do them all in one sitting: what you send is kept, and the link takes you back to what is left. If it has "
            + "expired, reply to this email and we will send a fresh one. " + NotExpecting;
        var html = Wrap(Paragraph(Hello(personName)) + Paragraph(Encode(opening))
            + $"<ol style=\"margin:0 0 14px;padding-left:20px\">{list}</ol>"
            + Paragraph($"{Encode(sentBy.Length > 0 ? sentBy : "The office")} has sent you this link. It is just for you.")
            + Button(link, "Open your forms") + SmallPrint(smallPrint));
        var text = $"Hello {FirstName(personName)}\n\n{opening}\n\n{string.Join("\n", formTitles.Select((title, index) => $"{index + 1}. {title}"))}"
            + $"\n\n{link}\n\nThe link lasts until {Day(expiresAt)}. If it has expired, reply to this email and we will send a fresh one. "
            + $"{NotExpecting}\n\n{FootLine}";
        var subject = $"Your new starter forms - {JewelBespokeBuild.LegalName}";
        return new FormEmail(new[] { email }, isReminder ? "Reminder: " + subject : subject, html, text);
    }

    private static string HowMany(int forms) => forms == 1 ? "one short form" : $"{Words(forms)} short forms";

    private static string Words(int number) => number switch
    {
        2 => "two", 3 => "three", 4 => "four", 5 => "five", 6 => "six",
        _ => number.ToString(CultureInfo.InvariantCulture)
    };
}
