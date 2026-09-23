using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// Right to work, the worker's half: this form collects, it does not check. The check is recorded
/// in the office by a named person, because the checker's record gives the statutory excuse and a
/// public page cannot say who the checker was. Only the two routes the office uses are offered (the
/// original passport seen in person, or a share code run online); there is no passport upload.
/// Evidence lives in the restricted right-to-work store. gov.uk rules checked 19 Aug 2026, and
/// again for this port on 23 Sep 2026.
/// </summary>
public static class RightToWorkForm
{
    public const string BritishOrIrishCitizen = "British or Irish citizen";
    public const string Employee = "Employee";

    public static readonly FormDefinition Definition = new(
        FormSlugs.RightToWork,
        "Right to Work Check",
        "This form tells us which check you need. It is not the check. We will still need to see your "
            + "original passport, or run your share code, before your first day. We do this for everybody, "
            + "including British citizens. Fields marked * are required.",
        new[]
        {
            Text("full_name", "Your full name", Required, "Exactly as it appears on your passport or ID."),
            Date("dob", "Date of birth", Required),
            Text("email", "Email", Required),
            Text("mobile", "Mobile", Required),
            Choice("status", "Which describes you", Required,
                new[] { BritishOrIrishCitizen, "Other" },
                "British and Irish citizens cannot get a share code, so we check your original passport with you "
                    + "present instead."),
            Text("share_code", "Share code (if Other)", Optional,
                "Get a free code at gov.uk/prove-right-to-work and put it here. It looks like W12 ABC 34D and "
                    + "lasts 90 days."),
            Date("start_date", "Date you are due to start", Optional),
            Choice("engaged_as", "Engaged as", Required, new[] { Employee, "Self-employed individual" }),
            Choice("company", "Company", Required,
                new[] { "Jewel Property Serve (JPS)", "Jewel Bespoke Build (JBB)" }),
            Signature("declaration", "Declaration", Required,
                "Signing here confirms that the details above are yours and are true, and that you will tell us "
                    + "if your permission to work changes.")
        },
        PrivacyNotice: "We use these details to carry out the right to work check the law requires before you start, "
            + "and to keep the record of that check. The record and any copy of your document are kept in our "
            + "secure records, seen only by the people who need to, retained two years after you leave us, "
            + "then destroyed. Contact the office to update your details or to ask about your data rights.",
        Store: FormEvidenceStore.RightToWork,
        FilingKeys: new[] { "full_name" });

    /// <summary>
    /// The check as far as the form can say it — who, for which company, how engaged, the route and the
    /// share code. The rest is the checker's to fill in, in the office, against their own name.
    /// </summary>
    public static RightToWorkCheckDetails CheckFrom(
        FormSubmission submission, IReadOnlyDictionary<string, string> answers, string checkerName, DateOnly today)
    {
        string Answer(string key) => answers.GetValueOrDefault(key, "").Trim();
        var typedName = Answer("full_name");
        var company = CompanyAnswered(Answer("company")) ?? submission.Company;
        var engagedAs = Answer("engaged_as") == Employee ? Engagement.Employee : Engagement.SelfEmployed;
        var route = Answer("status") == BritishOrIrishCitizen ? RightToWorkRoute.OriginalPassportSeen : RightToWorkRoute.ShareCode;
        return new RightToWorkCheckDetails(
            typedName.Length > 0 ? typedName : submission.SubmitterName, Answer("email"), company, "", engagedAs,
            FormDates.Read(Answer("start_date")), route, Answer("share_code"), "", RightToWorkSeenVia.InPerson, "", checkerName, today,
            false, false, false, false, false, null, null, RightToWorkOutcome.Pass, "", submission.FormSubmissionId);
    }

    /// <summary>The company the person said engages them — the form's own choice, named for the company it starts with.</summary>
    private static JewelCompany? CompanyAnswered(string answer) =>
        JewelCompanies.All.FirstOrDefault(particulars => answer.StartsWith(particulars.ShortName, StringComparison.OrdinalIgnoreCase))?.Company;
}
