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
        var checkedBy = check.CheckedByName.Length > 0 ? check.CheckedByName : "the office";
        var since = check.EngagedSince is { } engagedSince ? engagedSince.ToString("dd/MM/yyyy", British) : "your start date";
        var relationship = check.EngagedAs == Engagement.Employee ? "employed" : "engaged";
        var completed = $"Your right to work check was completed on {check.CheckedOn.ToString("dd/MM/yyyy", British)} by {checkedBy} "
            + $"using {check.Route.DisplayName()}. You have been {relationship} by {JewelBespokeBuild.LegalName} since {since}.";
        const string keptOnRecord = "We keep a record of the check as the law requires. Please tell us straight away if your permission to work changes.";
        var html = Wrap(Paragraph($"Hello {Encode(check.PersonName)},") + Paragraph(Encode(completed))
            + Paragraph(Encode(keptOnRecord)) + Paragraph(Encode(JewelBespokeBuild.LegalName)));
        var text = $"Hello {check.PersonName},\n\n{completed}\n\n{keptOnRecord}\n\n{JewelBespokeBuild.LegalName}";
        return new FormEmail(new[] { check.Email }, $"Right to work check completed - {JewelBespokeBuild.LegalName}", html, text);
    }
}
