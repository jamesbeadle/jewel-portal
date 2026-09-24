namespace Jewel.JPMS.Models;

/// <summary>
/// Where a form's files are kept. Right-to-work and payroll-starter evidence each have their own
/// restricted store, so access can be limited to the people who need it and the records can be
/// found and destroyed on their retention date without sifting through everything else. The
/// health and safety store holds the H&amp;S officer's site checks and the site's incident reports,
/// read by the office and the people on site who answer for safety (FormRoleSets).
/// </summary>
public enum FormEvidenceStore
{
    General = 0,
    RightToWork = 1,
    PayrollStarters = 2,
    HealthAndSafety = 3
}

/// <summary>Whether a form's answers are filed under the person who sent it, the company they sent it for,
/// or the site it was filled in on (a site check, an incident report).</summary>
public enum FormFilingKind
{
    Person = 0,
    Company = 1,
    Site = 2
}

/// <summary>
/// A form as Jeremy's forms dashboard defined it, ported rather than redesigned: the title, the
/// introduction, the questions in the order they are asked and the form's own privacy wording.
/// FilingKeys are the answers the person or company is named from when no one-time link names them.
/// IsAnAccidentReport sends the office alert to the accident addresses instead, the hour it lands.
/// IsSentByLinkOnly is a form that means nothing without what its sender chose (a policy sign-off):
/// its open address answers "not valid" and only a one-time link or a pack opens it.
/// </summary>
public sealed record FormDefinition(
    string Slug,
    string Title,
    string Intro,
    IReadOnlyList<FormQuestion> Questions,
    string PrivacyNotice = "",
    FormEvidenceStore Store = FormEvidenceStore.General,
    FormFilingKind FilingKind = FormFilingKind.Person,
    IReadOnlyList<string>? FilingKeys = null,
    bool IsAnAccidentReport = false,
    bool IsSentByLinkOnly = false)
{
    private static readonly string[] NamedByTheirName = { "name" };

    public IReadOnlyList<string> FilingKeys { get; init; } = FilingKeys ?? NamedByTheirName;

    public IReadOnlyList<FormQuestion> AskedQuestions => Questions.Where(question => question.IsAsked).ToList();

    public FormQuestion? QuestionFor(string key) => Questions.FirstOrDefault(question => question.Key == key);

    public string Privacy => PrivacyNotice.Length > 0 ? PrivacyNotice : FormWording.GeneralPrivacy(JewelBespokeBuild.LegalName);
}
