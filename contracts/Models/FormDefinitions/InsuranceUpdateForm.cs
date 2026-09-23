using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// The short annual insurance refresh for a sub-contractor already on the books, deliberately not
/// the whole questionnaire: one policy per submission, so a firm with public and employers
/// liability on separate policies sends it twice. The expiry date is the field that matters; it is
/// what the portal chases.
/// </summary>
public static class InsuranceUpdateForm
{
    public static readonly FormDefinition Definition = new(
        FormSlugs.InsuranceUpdate,
        "Sub-Contractor Insurance Update",
        "Please send us your current insurance certificate. It takes two minutes. If you hold more than "
            + "one policy, complete this once per policy. Fields marked * are required.",
        new[]
        {
            Text("company", "Company / Sole Trader Name", Required, "Exactly as it appears on the certificate."),
            Text("contact_name", "Your Full Name", Required),
            Text("email", "Email Address", Required, "We will send renewal reminders here."),
            Text("contact_number", "Contact Number", Optional),
            Choice("cover_type", "Type of cover", Required,
                new[]
                {
                    "Public Liability",
                    "Employers Liability",
                    "Product Liability",
                    "Professional Indemnity",
                    "Contractors All Risks",
                    "Combined policy (several covers)",
                    "Other"
                }),
            Text("insurer", "Insurer or broker", Required),
            Text("policy_ref", "Policy number", Required),
            Text("limit", "Limit of indemnity", Optional, "For example £5,000,000."),
            Date("expiry", "Policy expiry / renewal date", Required, "This is the date we will chase you on."),
            Upload("certificate", "Certificate or broker letter", Required,
                "A clear photo or PDF of the current certificate."),
            LongText("other_covers", "Any other policies you hold", Optional,
                "Tell us here and send a separate form for each."),
            LongText("notes", "Anything else we should know", Optional)
        },
        FilingKind: FormFilingKind.Company,
        FilingKeys: new[] { "company", "contact_name" });
}
