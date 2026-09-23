using static Jewel.JPMS.Api.Features.Forms.Mail.FormEmailFrame;

namespace Jewel.JPMS.Api.Features.Forms.Mail;

/// <summary>
/// The renewal chase, sent before the date rather than after it: a sub-contractor's insurance and a
/// person's training ticket, each with the form that answers it attached as a one-time link, so
/// replying is one tap. The insurance form tells the person their expiry date will be used to
/// remind them; this is that reminder.
/// </summary>
internal static class FormRenewalEmails
{
    public static FormEmail ForInsurance(
        JewelCompany company, string contactName, string email, string cover, DateOnly expiresOn, string link, DateTimeOffset linkExpiresAt)
    {
        var ask = $"Our records show your {cover.ToLowerInvariant()} expires on {Day(expiresOn)}. Please send us the renewed "
            + "certificate before then, so there is no gap in your cover on our sites. It takes two minutes on your phone.";
        return Chase(company, contactName, email, $"Your {cover.ToLowerInvariant()} renewal", ask, "Send the renewal", link, linkExpiresAt);
    }

    public static FormEmail ForTraining(
        JewelCompany company, string personName, string email, string course, DateOnly expiresOn, string link, DateTimeOffset linkExpiresAt)
    {
        var ask = $"Our records show your {course} expires on {Day(expiresOn)}. When you have renewed it, please send us the new "
            + "certificate or card so our training records stay right. It takes two minutes on your phone.";
        return Chase(company, personName, email, $"Your {course} renewal", ask, "Send the new certificate", link, linkExpiresAt);
    }

    private static FormEmail Chase(
        JewelCompany company, string personName, string email, string subject, string ask, string buttonLabel, string link, DateTimeOffset linkExpiresAt)
    {
        var particulars = JewelCompanies.For(company);
        var smallPrint = $"The link works until <b>{Encode(Day(linkExpiresAt))}</b> and can only be used once. If it has expired, "
            + "reply to this email and we will send another. If you were not expecting this, please tell us and do not use the link.";
        var html = Wrap(company, Paragraph(Hello(personName)) + Paragraph(Encode(ask)) + Button(company, link, buttonLabel) + SmallPrint(smallPrint));
        var text = $"Hello {FirstName(personName)}\n\n{ask}\n\n{link}\n\nThe link works until {Day(linkExpiresAt)} and can only be used once."
            + $"\n\n{FootLine(particulars)}";
        return new FormEmail(company, new[] { email }, $"{subject} - {particulars.LegalName}", html, text);
    }
}
