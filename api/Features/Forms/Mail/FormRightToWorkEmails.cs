using System.Globalization;
using static Jewel.JPMS.Api.Features.Forms.Mail.FormEmailFrame;

namespace Jewel.JPMS.Api.Features.Forms.Mail;

/// <summary>
/// The "check completed" email the register sends a person once their check is a completed pass
/// (api/registers.js op:'rtwconfirm'), word for word: when, by whom, by which route, and since when
/// they have been employed or engaged — a subcontractor is engaged, never employed.
/// </summary>
internal static class FormRightToWorkEmails
{
    private static readonly CultureInfo British = CultureInfo.GetCultureInfo("en-GB");

    public static FormEmail CheckCompleted(RightToWorkCheckDetails check)
    {
        var particulars = JewelCompanies.For(check.Company);
        var company = particulars.LegalName;
        var checkedBy = check.CheckedByName.Length > 0 ? check.CheckedByName : "the office";
        var since = check.EngagedSince is { } engagedSince ? engagedSince.ToString("dd/MM/yyyy", British) : "your start date";
        var relationship = check.EngagedAs == Engagement.Employee ? "employed" : "engaged";
        var completed = $"Your right to work check was completed on {check.CheckedOn.ToString("dd/MM/yyyy", British)} by {checkedBy} "
            + $"using {check.Route.DisplayName()}. You have been {relationship} by {company} since {since}.";
        const string keptOnRecord = "We keep a record of the check as the law requires. Please tell us straight away if your permission to work changes.";
        var html = Wrap(check.Company, Paragraph($"Hello {Encode(check.PersonName)},") + Paragraph(Encode(completed))
            + Paragraph(Encode(keptOnRecord)) + Paragraph(Encode(company)));
        var text = $"Hello {check.PersonName},\n\n{completed}\n\n{keptOnRecord}\n\n{company}";
        return new FormEmail(check.Company, new[] { check.Email }, $"Right to work check completed - {company}", html, text);
    }
}
