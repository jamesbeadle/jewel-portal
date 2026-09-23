using Jewel.JPMS.Api.Features.Forms.Answers;
using static Jewel.JPMS.Api.Features.Forms.Mail.FormEmailFrame;

namespace Jewel.JPMS.Api.Features.Forms.Mail;

/// <summary>
/// The two emails a sent form raises (api/forms-intake.js): the person's own copy, when a one-time
/// link says who they are — a person who cannot show what they sent has little to argue with later —
/// and the office's alert. Neither ever carries a sensitive answer, and a form kept in a restricted
/// store alerts the office without its answers, because the alert goes wider than the store's readers.
/// </summary>
internal static class FormReceiptEmails
{
    public static FormEmail CopyForThePerson(
        JewelCompany company, FormDefinition form, string personName, string email, IReadOnlyList<FormAnswerLine> lines, IReadOnlyList<string> fileNames)
    {
        var particulars = JewelCompanies.For(company);
        var title = form.TitleFor(company);
        var html = Wrap(company,
            Paragraph($"Thank you {Encode(FirstName(personName))}. We have received your {Encode(title)}.")
            + "<p style=\"margin:0 0 8px\"><b>This is what you sent us:</b></p>" + Table(lines) + Files("Files you attached", fileNames)
            + SmallPrint(Encode(FormWording.CopyLeavesThingsOut(particulars.Phone))));
        var text = $"Thank you {FirstName(personName)}. We have received your {title}.\n\nThis is what you sent us:\n\n"
            + string.Join("\n", lines.Select(line => $"{line.Label}: {line.Answer}"))
            + $"\n\n{FormWording.CopyLeavesThingsOut(particulars.Phone)}\n\n{FootLine(particulars)}";
        return new FormEmail(company, new[] { email }, "Your copy: " + title, html, text);
    }

    public static FormEmail AlertForTheOffice(
        JewelCompany company, FormDefinition form, string who, IReadOnlyList<string> to, IReadOnlyList<FormAnswerLine> lines,
        IReadOnlyList<string> fileNames, string officeLink)
    {
        var title = form.TitleFor(company);
        var isAccident = form.Slug == FormSlugs.AccidentReport;
        var subject = isAccident ? $"Accident reported: {who}" : $"Form in: {title} - {who}";
        var heading = isAccident ? "A vehicle accident has just been reported" : $"{title} - new submission";
        var html = Wrap(company,
            $"<h3 style=\"margin:0 0 8px\">{Encode(heading)}</h3>" + Paragraph($"<b>{Encode(who)}</b>")
            + Table(lines) + Files("Files", fileNames)
            + Paragraph($"<a href=\"{Encode(officeLink)}\">Open it in the portal</a>"));
        var text = $"{heading}\n\n{who}\n\n" + string.Join("\n", lines.Select(line => $"{line.Label}: {line.Answer}"))
            + $"\n\nOpen it in the portal: {officeLink}";
        return new FormEmail(company, to, subject, html, text);
    }

    private static string Table(IReadOnlyList<FormAnswerLine> lines)
    {
        var rows = lines.Select(line =>
            $"<tr><td style=\"color:#6b7280;padding:3px 12px 3px 0;vertical-align:top\">{Encode(line.Label)}</td>"
            + $"<td style=\"vertical-align:top\">{Encode(line.Answer).Replace("\n", "<br>")}</td></tr>");
        return $"<table style=\"font-size:14px;border-collapse:collapse\">{string.Concat(rows)}</table>";
    }

    private static string Files(string heading, IReadOnlyList<string> fileNames)
    {
        if (fileNames.Count == 0) return "";
        var items = string.Concat(fileNames.Select(name => $"<li>{Encode(name)}</li>"));
        return $"<p style=\"margin:14px 0 4px\"><b>{Encode(heading)}</b></p><ul style=\"font-size:14px;margin:0;padding-left:20px\">{items}</ul>";
    }
}
