using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Registers;

/// <summary>What accepting a certificate writes: the dates, the provider and number the form gave, the certificate, and a fresh chase count.</summary>
internal static class TrainingRecordWriting
{
    private const int LongestField = 256;
    private const int LongestNumber = 128;

    public static void Accept(
        TrainingRecordEntity record, AcceptTrainingCertificate command, FormSubmissionEntity submission,
        IReadOnlyDictionary<string, string> answers, string? certificateUploadId)
    {
        record.PersonName = Clip(command.PersonName, LongestField);
        record.Course = Clip(command.Course, LongestField);
        record.CompletedOn = command.CompletedOn;
        record.ExpiresOn = command.ExpiresOn;
        record.Provider = Clip(answers.GetValueOrDefault("provider", record.Provider), LongestField);
        record.CertificateNumber = Clip(answers.GetValueOrDefault("cert_no", record.CertificateNumber), LongestNumber);
        record.Email = submission.SentToEmail.Length > 0 ? submission.SentToEmail : record.Email;
        record.CertificateUploadId = certificateUploadId ?? record.CertificateUploadId;
        record.FormSubmissionId = submission.FormSubmissionId;
        record.AcceptedByEmail = command.AcceptedByEmail;
        record.AcceptedAt = DateTimeOffset.UtcNow;
        record.LastChasedAt = null;
        record.ChaseCount = 0;
        record.EndedOn = null;
    }

    private static string Clip(string value, int longest)
    {
        var trimmed = value.Trim();
        return trimmed.Length > longest ? trimmed[..longest] : trimmed;
    }
}
