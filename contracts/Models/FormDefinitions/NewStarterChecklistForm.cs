using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// HMRC's own starter checklist put on a phone, so a new employee with no P45 is not put on an
/// emergency code. Verified against HMRC's PDF on 24 Aug 2026: last name and first names asked
/// separately with no initials, because that is what goes on the FPS; sex asked with Male and
/// Female only, exactly as HMRC asks it, to match the National Insurance record, and nothing wider;
/// statements A, B and C in HMRC's wording; the postgraduate loan asked apart from the plan because
/// it is repaid alongside one. Bank details are deliberately not asked: payroll mandate fraud works
/// exactly that way. The answers live in the restricted payroll-starters store.
/// </summary>
public static class NewStarterChecklistForm
{
    private static readonly string[] Statements =
    {
        "A - This is my first job since 6 April, and since 6 April I have not received Jobseeker's "
            + "Allowance, Employment and Support Allowance or Incapacity Benefit",
        "B - Since 6 April I have had another job, or received Jobseeker's Allowance, Employment and "
            + "Support Allowance or Incapacity Benefit, but I do not have a P45",
        "C - I have another job, or I receive a State, workplace or private pension"
    };

    public static readonly FormDefinition Definition = new(
        FormSlugs.NewStarter,
        "New Starter Checklist",
        "For your first pay. If you have a P45 from a job in this tax year, send us that instead and "
            + "skip this. Fields marked * are required.",
        new[]
        {
            Choice("title", "Title", Optional, new[] { "Mr", "Mrs", "Miss", "Ms", "Mx", "Dr" }),
            Text("first_name", "First names", Required,
                "In full - no initials or shortened names. This is what goes to HMRC."),
            Text("last_name", "Last name", Required),
            Text("previous_name", "Any previous name", Optional,
                "Only if your payroll or student loan records might be under a different name."),
            Choice("sex", "What is your sex?", Required,
                new[] { "Male", "Female" },
                "Asked exactly as HMRC asks it, to match your National Insurance record. It goes to payroll and "
                    + "nowhere else."),
            Date("dob", "Date of birth", Required),
            Text("ni_number", "National Insurance number", Required,
                "On your payslip, your NI card or in your HMRC app. Nine characters, like QQ123456C."),
            LongText("address", "Home address", Required, "Including postcode. This is where HMRC will write to you."),
            Text("mobile", "Mobile number", Required, "The number the office should use to reach you."),
            Text("email", "Personal email address", Optional,
                "If this form reached you by email we may have it already. Fill it in if that address was a work "
                    + "one, or someone else's."),
            Date("start_date", "Date you started, or are starting, with us", Required),
            Choice("statement", "Which one of these is true for you", Required,
                Statements,
                "Pick the one that describes you today. If you are not sure, pick the closest and tell us in the "
                    + "notes: it is easy for us to correct, and much harder for you to get overpaid tax back later."),
            Choice("student_loan", "Do you have a student loan that is not fully repaid", Required,
                new[] { "No", "Yes" },
                "Say no if you have finished paying it off, or if the Student Loans Company has written to you "
                    + "to say it is settled."),
            Choice("loan_plan", "If yes, which plan", Optional,
                new[] { "", "Plan 1", "Plan 2", "Plan 4", "Plan 5", "I do not know" },
                "Plan 1 Northern Ireland, or England and Wales before September 2012. Plan 2 England 2012 to "
                    + "July 2023, Wales from 2012. Plan 4 Scotland. Plan 5 England from August 2023. If you do not "
                    + "know, say so and we will not guess."),
            Choice("pg_loan", "Do you have a postgraduate loan that is not fully repaid", Required,
                new[] { "No", "Yes" },
                "A masters or doctoral loan. It is separate and is repaid at the same time as the plan above."),
            Signature("declaration", "Declaration", Required,
                "Signing here confirms the information above is correct to the best of your knowledge, and that "
                    + "you will tell us if it changes."),
            LongText("notes", "Anything we should know", Optional)
        },
        Store: FormEvidenceStore.PayrollStarters,
        FilingKeys: new[] { "first_name", "last_name" });
}
