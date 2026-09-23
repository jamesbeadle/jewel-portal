using static Jewel.JPMS.Models.Ask;

namespace Jewel.JPMS.Models;

/// <summary>
/// Training certificate, the worker's half. The office accepts it onto the training register with
/// the dates the form collected, so the expiry is chased rather than left inside an image; the
/// public form never writes the register itself.
/// </summary>
public static class TrainingCertificateForm
{
    public static readonly FormDefinition Definition = new(
        FormSlugs.TrainingCertificate,
        "Training Certificate",
        "Please send us a photo or PDF of your certificate or card for the course you have just "
            + "completed. One course per form. Fields marked * are required.",
        new[]
        {
            Text("name", "Your Full Name", Required),
            Text("course", "Course or qualification", Required,
                "As it appears on the certificate, for example Asbestos Awareness, CSCS, First Aid at Work, "
                    + "SMSTS."),
            Text("provider", "Training provider", Optional,
                "Who ran the course, for example High Speed Training, CITB."),
            Date("completed", "Date completed", Required),
            Date("expiry", "Expiry date (if shown)", Optional, "Leave blank if the certificate has no expiry."),
            Text("cert_no", "Certificate or card number (if shown)", Optional),
            Upload("certificate", "Photo or PDF of the certificate", Required,
                "Both sides if it is a card. Make sure your name and the date are readable."),
            LongText("notes", "Anything else we should know", Optional)
        },
        PrivacyNotice: "We use this to keep our training records up to date and to show clients and the HSE that people "
            + "on site hold the right qualifications. The certificate is stored in our secure records while "
            + "you work with us and for three years afterwards, after which it is archived. Contact the office "
            + "to ask about your data rights.");

    /// <summary>The register entry as far as the form says it: whose certificate, which course, and its dates.</summary>
    public static TrainingCertificateDetails DetailsFrom(FormSubmission submission, IReadOnlyDictionary<string, string> answers)
    {
        string Answer(string key) => answers.GetValueOrDefault(key, "").Trim();
        var typedName = Answer("name");
        return new TrainingCertificateDetails(
            typedName.Length > 0 ? typedName : submission.SubmitterName, Answer("course"),
            FormDates.Read(Answer("completed")), FormDates.Read(Answer("expiry")));
    }
}

/// <summary>What a Training Certificate form says the certificate is, for the office to check before it is accepted.</summary>
public sealed record TrainingCertificateDetails(string PersonName, string Course, DateOnly? CompletedOn, DateOnly? ExpiresOn);
