using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// A paper register's ruled rows on a form (2026-09-23, Katy-Louise's site checks): a table answer
// is one JSON string of rows, kept only as the table's own columns, a required table needs a row
// somebody wrote on, a checklist keeps exactly the sheet's rows, and the rows read as sentences.
public sealed class FormTableTests
{
    private static readonly FormTable Attendance = ToolboxTalkRegisterForm.Definition.QuestionFor("attendance")!.Table!;
    private static readonly FormTable Contents = FirstAidKitChecklistForm.Definition.QuestionFor("contents")!.Table!;

    [Fact]
    public void AFreeTable_keepsItsOwnColumns_dropsRowsNobodyWroteOn_andCapsCells()
    {
        var posted = FormTableAnswers.Write(new IReadOnlyDictionary<string, string>[]
        {
            new Dictionary<string, string> { ["attendee"] = " Sam Jones ", ["company"] = "JBB", ["signature"] = "SJ", ["rogue"] = "x" },
            new Dictionary<string, string> { ["attendee"] = "" },
            new Dictionary<string, string> { ["attendee"] = new string('a', 600) }
        });

        var kept = FormTableAnswers.Cleaned(Attendance, posted);

        Assert.Equal(2, kept.Count);
        Assert.Equal("Sam Jones", kept[0]["attendee"]);
        Assert.False(kept[0].ContainsKey("rogue"));
        Assert.Equal(FormTableAnswers.LongestCell, kept[1]["attendee"].Length);
    }

    [Fact]
    public void AChecklist_keepsTheSheetsRowsInOrder_withItsLabelsReimposed()
    {
        var posted = FormTableAnswers.Write(new IReadOnlyDictionary<string, string>[]
        {
            new Dictionary<string, string> { ["item"] = "Something else", ["suitable"] = FormWording.Yes }
        });

        var kept = FormTableAnswers.Cleaned(Contents, posted);

        Assert.Equal(FirstAidKitContents.Items.Count, kept.Count);
        Assert.Equal("Guidance leaflet", kept[0]["item"]);
        Assert.Equal(FormWording.Yes, kept[0]["suitable"]);
        Assert.Equal("Medium sterile dressing", kept[1]["item"]);
        Assert.False(kept[1].ContainsKey("suitable"));
    }

    [Fact]
    public void ARequiredTable_needsARowSomebodyWroteOn()
    {
        var form = ToolboxTalkRegisterForm.Definition;
        var answers = new Dictionary<string, string>
        {
            ["company"] = "JBB", ["site"] = "By France", ["talk"] = ToolboxTalkTopics.All[0], ["attendance"] = "[{}]"
        };
        var problem = FormAnswerRules.FirstProblem(form, answers, new Dictionary<string, int>());
        Assert.Equal(FormWording.PleaseFillIn("Attendance"), problem);

        answers["attendance"] = FormTableAnswers.Write(new IReadOnlyDictionary<string, string>[] { new Dictionary<string, string> { ["attendee"] = "Sam" } });
        var next = FormAnswerRules.FirstProblem(form, answers, new Dictionary<string, int>());
        Assert.Equal(FormWording.PleaseFillIn("Name"), next);
    }

    [Fact]
    public void TheRows_readAsSentences_andBadJsonIsNoRows()
    {
        var json = FormTableAnswers.Write(new IReadOnlyDictionary<string, string>[]
        {
            new Dictionary<string, string> { ["attendee"] = "Sam Jones", ["company"] = "JBB" }
        });
        Assert.Equal("1. Name (print): Sam Jones · Company: JBB", FormTableAnswers.Sentence(Attendance, json));
        Assert.Empty(FormTableAnswers.Read("not json"));
        Assert.Equal("", FormTableAnswers.Write(Array.Empty<IReadOnlyDictionary<string, string>>()));
    }

    [Fact]
    public void TheEightSheets_areInTheHealthAndSafetyStore_filedBySite_andTheIncidentReportsAlert()
    {
        Assert.All(FormCatalogue.HealthAndSafety, form => Assert.Equal(FormEvidenceStore.HealthAndSafety, form.Store));
        Assert.True(FormCatalogue.For(FormSlugs.SiteIncident)!.IsAnAccidentReport);
        Assert.True(FormCatalogue.For(FormSlugs.PersonnelIncident)!.IsAnAccidentReport);
        Assert.False(FormCatalogue.For(FormSlugs.ToolboxTalk)!.IsAnAccidentReport);
        Assert.True(SensitiveAnswers.IsSensitive(PersonnelIncidentReportForm.Definition.QuestionFor("injury")!));
        Assert.True(SensitiveAnswers.IsSensitive(PersonnelIncidentReportForm.Definition.QuestionFor("ni_number")!));
        Assert.Equal(115, ToolboxTalkTopics.All.Length);
        Assert.True(FormRoleSets.HealthAndSafetyReaders.Includes(Role.HealthSafetyOfficer));
        Assert.False(FormRoleSets.Office.Includes(Role.HealthSafetyOfficer));
    }
}
