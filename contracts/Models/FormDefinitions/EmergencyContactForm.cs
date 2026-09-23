using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// Who to contact in an emergency. The health questions were rewritten on 16 Aug 2026: both
/// optional and narrowed to what an ambulance crew or a site supervisor would need, because blanket
/// pre-engagement health questions are restricted under the Equality Act 2010. The two answers are
/// health data (UK GDPR Article 9): marked special category, never emailed back, and shown in the
/// office only when revealed, each reveal on the audit trail.
/// </summary>
public static class EmergencyContactForm
{
    public static readonly FormDefinition Definition = new(
        FormSlugs.EmergencyContact,
        "Emergency Contact Form",
        "Who should we contact in an emergency, and anything we should know to keep you safe at work.",
        new[]
        {
            Text("name", "Your Full Name", Required),
            Text("ec_name", "Emergency contact's full name", Required),
            Text("ec_email", "Emergency contact's email address", Required),
            Text("ec_phone", "Emergency contact's phone number", Required),
            LongText("ec_address", "Emergency contact's home address", Required),
            Text("relationship", "Relationship to you", Required),
            LongText("medical_detail", "Anything a paramedic should know in an emergency?", Optional,
                "Optional. For example a serious allergy, epilepsy, diabetes, a heart condition, or medication "
                    + "you carry. Leave blank if there is nothing.") with { IsSpecialCategory = true },
            LongText("support_detail", "Anything that would help us keep you safe or comfortable at work?", Optional,
                "Optional. For example an old injury that limits lifting, hearing difficulty on a noisy site, or "
                    + "an adjustment that would help. We will talk to you before acting on it.") with { IsSpecialCategory = true }
        });
}
