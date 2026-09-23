using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// G-01 Toolbox Talk Register (Akeva template, Site Managers Working File), as the paper sheet asks
/// it and no more: the header, the declaration every attendee signs to, the attendance rows, the
/// presenter's sign-off and the consultation box. Filed under the site. The talk is chosen from the
/// G-02 / G-03 lists, which give its title and number together.
/// </summary>
public static class ToolboxTalkRegisterForm
{
    public const string Declaration =
        "I have been given a toolbox talk by the presenter named below and I fully understand its contents. I was "
        + "also given the chance to mention any points or concerns I may have had about the topic.";

    private static readonly FormTable Attendance = new(
        new[]
        {
            Column.Text("attendee", "Name (print)"),
            Column.Text("company", "Company"),
            Column.Text("signature", "Signature (type your name to sign)")
        },
        "attendee");

    public static readonly FormDefinition Definition = new(
        FormSlugs.ToolboxTalk,
        "Toolbox Talk Register",
        "The G-01 toolbox talk register, filled in on the portal instead of the paper sheet. Fields marked * are required.",
        new[]
        {
            Text("company", "Company", Required),
            Text("site", "Site", Required),
            Choice("talk", "Title and number of toolbox talk", Required, ToolboxTalkTopics.All,
                "Akeva's G-02 general and G-03 environmental lists."),
            Text("further_talks", "Any further talk(s) given at the same time", Optional, "Title and number, as above."),
            Section("_sec_declaration", "Declaration", Declaration),
            Table("attendance", "Attendance", Required, Attendance),
            Section("_sec_presenter", "Presenter", "I confirm the above persons have attended."),
            Text("presenter_name", "Name", Required),
            Text("presenter_position", "Position", Required),
            Date("presenter_date", "Date", Required),
            Signature("presenter_signature", "Signature", Required),
            Section("_sec_consultation", "Consultation with employees"),
            LongText("consultation", "Comments made and answers given", Optional),
            Text("consultation_signed", "Signed", Optional),
            Date("consultation_date", "Date", Optional)
        },
        Store: FormEvidenceStore.HealthAndSafety,
        FilingKind: FormFilingKind.Site,
        FilingKeys: new[] { "site" });
}
