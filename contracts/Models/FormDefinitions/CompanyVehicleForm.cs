using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// The company vehicle form, rebuilt to Jeremy's revised field spec on 25 Aug 2026. Date of birth,
/// mobile and overnight postcode are insurer requirements, and the age rule is stated so nobody
/// fills it in for nothing. The DVLA check code replaces trusting the photo alone (the office views
/// the real record within 21 days). Only UNSPENT convictions are asked, per the Rehabilitation of
/// Offenders Act 1974. The driving-record answers are criminal offence and health data: they are
/// never echoed in the copy emailed to the signer, and the privacy paragraph states their narrow
/// use. Keys name, licence, test_date, points and points_detail are kept as the dashboard had them.
/// </summary>
public static class CompanyVehicleForm
{
    public static readonly FormDefinition Definition = new(
        FormSlugs.CompanyVehicle,
        "Company Vehicle Form",
        "Before we can allocate you a company vehicle we need your licence details, a DVLA check code "
            + "and a signed declaration. It takes about five minutes. Fields marked * are required.",
        new[]
        {
            Section("_sec_you", "Your details"),
            Text("name", "Full name (as shown on your licence)", Required),
            Date("dob", "Date of birth", Required,
                "Our insurance does not cover drivers aged 29 or under. We cannot allocate a vehicle until you "
                    + "are 30."),
            Text("mobile", "Mobile number", Required),
            Text("postcode", "Postcode where the vehicle will be parked overnight", Required),
            Section("_sec_licence", "Your licence"),
            Upload("licence", "Photo of your driving licence", Required, "Clear picture of the front and back."),
            Text("licence_last8", "Last 8 characters of your licence number", Required),
            Choice("uk_licence", "Is your licence issued in the UK?", Required, new[] { "Yes", "No" }),
            Date("test_date", "When did you pass your driving test?", Required),
            Section("_sec_dvla", "DVLA check code",
                "Go to gov.uk/view-driving-licence → \"Share your licence information\" → generate a code. You'll "
                    + "need your licence number, National Insurance number and postcode. The code lasts 21 days."),
            Text("dvla_code", "DVLA check code", Required),
            Section("_sec_record", "Driving record"),
            Choice(
                "points",
                "Do you currently have any points, endorsements or convictions on your licence?",
                Required,
                new[] { "Yes", "No" }),
            LongText("points_detail", "If yes, give details", Optional) with { ShownWhen = new("points", "Yes") },
            Choice("disqualified", "Have you ever been disqualified from driving?", Required, new[] { "Yes", "No" }),
            LongText("disqualified_detail", "If yes, give details", Optional) with { ShownWhen = new("disqualified", "Yes") },
            Choice(
                "tacho",
                "In the last three years, have you been convicted of any tachograph or drivers' hours offence, "
                    + "or had an operator's licence revoked or restricted?",
                Required,
                new[] { "Yes", "No" }),
            Choice(
                "unspent",
                "Do you have any unspent criminal conviction or police caution for a non-motoring offence?",
                Required,
                new[] { "Yes", "No" }),
            Choice("medical", "Do you have any medical condition that must be notified to the DVLA?", Required,
                new[] { "Yes", "No" }),
            Choice("eyesight", "Do you need glasses or contact lenses to drive?", Required, new[] { "Yes", "No" }),
            Section("_sec_declaration", "Declaration", "All four boxes must be ticked."),
            Declaration("dec_accurate", "The information above is accurate and complete.", Required),
            Declaration(
                "dec_notify",
                "I will tell the office immediately, and in any event within 7 days, if I receive any points, "
                    + "endorsement or disqualification, or if my licence status, medical fitness to drive, or "
                    + "overnight parking address changes. I understand I must stop driving company vehicles "
                    + "immediately if I am disqualified, and that failure to notify may affect the company's insurance "
                    + "and is a breach of my subcontract terms.",
                Required),
            Declaration(
                "dec_goods",
                "I understand the vehicle is insured for carriage of the company's own goods only, and must not "
                    + "be used to carry goods or passengers for hire or reward.",
                Required),
            Declaration("dec_policy", "I have read and accept the Company Vehicle Policy.", Required),
            Signature("declaration", "Signature", Required,
                "Type your full name and draw your signature. The date is recorded automatically when you submit.")
        },
        PrivacyNotice: "We use these details to confirm you are entitled to drive, to meet the conditions of our motor "
            + "insurance, and to meet our health and safety duties. Where required, we share them with our "
            + "insurers and insurance brokers. Where your record shows motoring convictions, we record only "
            + "whether you meet our insurance criteria — not the detail of the offence — unless we are "
            + "required to disclose it to our insurer. We keep your licence details while you hold a company "
            + "vehicle and for six years afterwards for insurance purposes. The photo of your licence is "
            + "deleted once we have verified it. Contact the office to update your details or to ask about "
            + "your data rights.");
}
