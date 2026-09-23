using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Mail;

using static Jewel.JPMS.Api.Features.Forms.Mail.FormEmailFrame;

namespace Jewel.JPMS.Api.Features.Hs.Notifications;

/// <summary>What the digest needs to say about the project: its name, each action's words, and how many are still open.</summary>
public sealed record HsDigestContext(string ProjectName, IReadOnlyDictionary<string, string> SummariesByRecord, int OpenActionCount, string ActionsLink);

/// <summary>
/// The digest as a person reads it: one email listing every action touched in the sitting, what
/// happened on each and who did it, then how many actions the site still has open and the link
/// to the H&amp;S tab where the thread is. Jewel Bespoke Build's frame, like every portal email.
/// </summary>
public static class HsDigestEmails
{
    public static FormEmail Compose(HsDigest digest, HsDigestContext context)
    {
        var heading = HeadingFor(digest, context.ProjectName);
        var byRecord = digest.Events.GroupBy(occurrence => occurrence.HsRecordId).ToList();
        var subject = $"H&S actions on {context.ProjectName}: {Counted(byRecord.Count, "action")} updated";
        var html = Wrap(
            $"<h3 style=\"margin:0 0 8px\">{Encode(heading)}</h3>"
            + string.Concat(byRecord.Select(record => RecordHtml(record.Key, record.ToList(), context)))
            + Paragraph(Encode(StillOpen(context)))
            + Paragraph($"<a href=\"{Encode(context.ActionsLink)}\">Open the H&S actions in the portal</a>"));
        var text = heading + "\n\n"
            + string.Join("\n\n", byRecord.Select(record => RecordText(record.Key, record.ToList(), context)))
            + $"\n\n{StillOpen(context)}\n{context.ActionsLink}";
        return new FormEmail(new[] { digest.To }, subject, html, text);
    }

    public static string Line(HsRecordEventEntity occurrence)
    {
        var who = occurrence.ByName.Length > 0 ? occurrence.ByName : occurrence.ByEmail;
        return (HsRecordEventKind)occurrence.Kind switch
        {
            HsRecordEventKind.Raised => $"Raised from audit {occurrence.Detail} by {who}",
            HsRecordEventKind.Commented => $"{who} wrote: {occurrence.Detail}",
            _ => $"Status {occurrence.Detail} by {who}"
        };
    }

    private static string HeadingFor(HsDigest digest, string projectName) => digest.Audience switch
    {
        HsDigestAudience.SiteManager => $"The H&S officer has been through the actions on {projectName}",
        _ => $"The site has written on the H&S actions on {projectName}"
    };

    private static string RecordHtml(string hsRecordId, IReadOnlyList<HsRecordEventEntity> events, HsDigestContext context)
    {
        var lines = string.Concat(events.Select(occurrence => $"<li>{Encode(Line(occurrence))}</li>"));
        return $"<p style=\"margin:14px 0 4px\"><b>{Encode(SummaryOf(hsRecordId, context))}</b></p>"
            + $"<ul style=\"font-size:14px;margin:0;padding-left:20px\">{lines}</ul>";
    }

    private static string RecordText(string hsRecordId, IReadOnlyList<HsRecordEventEntity> events, HsDigestContext context) =>
        SummaryOf(hsRecordId, context) + "\n" + string.Join("\n", events.Select(occurrence => "- " + Line(occurrence)));

    private static string SummaryOf(string hsRecordId, HsDigestContext context) =>
        context.SummariesByRecord.GetValueOrDefault(hsRecordId, "An action that has since been removed");

    private static string StillOpen(HsDigestContext context) =>
        context.OpenActionCount == 0 ? "No action is open on this site." : $"Still open on this site: {Counted(context.OpenActionCount, "action")}.";

    private static string Counted(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";
}
