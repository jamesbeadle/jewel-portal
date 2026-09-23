using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// Jewel Bespoke Build's sub-contractor questionnaire, "Sub Contractor Questionnaire 2025" as the JBB
/// book's form in Jeremy's dashboard asks it. The UTR, NI number or company registration number are
/// for CIS verification; the expiry dates are asked as dates so the portal can chase them.
/// </summary>
public static class SubcontractorQuestionnaireForm
{
    public static readonly FormDefinition Definition = new(
        FormSlugs.SubcontractorQuestionnaire,
        "Sub Contractor Questionnaire 2025",
        "Please complete this questionnaire so we can set you up as an approved sub-contractor. Fields "
            + "marked * are required.",
        new[]
        {
            Text("company", "Company / Sole Trader Name", Required),
            LongText("address", "Full Address", Required),
            Choice("legal_form", "Limited Company, Partner or Sole Trader", Required,
                new[] { "Limited Company", "Partnership", "Sole Trader" }),
            Text("trade", "Specialist Trade", Required, "Carpenter, electrician, plumber etc."),
            Text("contact_name", "Contact Full Name", Required),
            Text("contact_number", "Contact Number", Required),
            Text("email", "Email Address", Required),
            Text("utr", "UTR Number", Required, "Needed for CIS verification."),
            Text("ni_crn", "NI number (sole trader) or Company Registration Number", Required),
            Upload("qualifications", "Qualifications", Optional, "Attach any qualifications or tickets you hold."),
            Choice(
                "hs_notice",
                "Have you been served with any improvement notices or been prosecuted for a Health & Safety or "
                    + "any other offence in the last 3 years?",
                Required,
                new[] { "No", "Yes" }),
            LongText("hs_notice_detail", "If yes, give details", Optional),
            Upload("hs_policy", "Health & Safety policy", Optional, "If you have one, attach a current copy."),
            Upload("trade_org", "Are you a member of, or accredited by, a trade organisation?", Optional,
                "If yes, attach certificate copies and give details in the box at the end."),
            Upload("id_photo", "Photo of your ID / Passport", Required),
            Upload("dbs", "DBS record check", Optional, "If appropriate for your trade."),
            Choice("insurance_confirm", "Do you currently hold valid insurance?", Required, new[] { "Yes", "No" }),
            Upload("insurance_docs", "Insurance certificates or broker letters", Required,
                "Public Liability, plus Product Liability, Employers Liability and Professional Indemnity where "
                    + "appropriate."),
            Text("pl_insurer", "Public Liability insurer or broker", Optional),
            Text("pl_policy_ref", "Public Liability policy number", Optional),
            Date("pl_expiry", "Public Liability expiry / renewal date", Optional,
                "We will remind you before this date. Leave blank only if you hold no insurance."),
            Date("el_expiry", "Employers Liability expiry date (if held)", Optional),
            Upload("references", "Two references from similar jobs in the past 12 months", Optional),
            LongText(
                "appoint_subs",
                "Do you appoint sub-contractors yourself? If yes, how do you assess their competence?",
                Optional),
            Choice("uniform", "Uniform size", Required, new[] { "S", "M", "L", "XL", "XXL" }),
            Signature("policies_sig", "Our policies", Required,
                "The office will send you our policies and expectations document. Signing here confirms you have "
                    + "read and agree to it."),
            Signature("nda_sig", "NDA and Terms & Conditions", Required,
                "Signing here confirms you agree to our NDA and terms and conditions."),
            LongText("notes", "Anything else we should know", Optional)
        },
        FilingKind: FormFilingKind.Company,
        FilingKeys: new[] { "company", "contact_name" });
}
