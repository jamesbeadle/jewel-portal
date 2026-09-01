using Jewel.JPMS.Contracts.DocumentControl;

namespace Jewel.JPMS.Api.Features.DocumentControl.Commands;

public sealed class FileDocumentAsPaymentCertificateValidation
{
    public ValidationOutcome Check(FileDocumentAsPaymentCertificate command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.DocumentControlItemId)) errors.Add("DocumentControlItemId is required.");
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        if (string.IsNullOrWhiteSpace(command.CertificateNumber)) errors.Add("Certificate number is required.");
        else if (command.CertificateNumber.Trim().Length > 64) errors.Add("Certificate number must be 64 characters or fewer.");
        if (command.CertifiedAmount is < 0) errors.Add("Certified amount cannot be negative.");
        if (command.IssuedDate == default) errors.Add("Issued date is required.");
        if (errors.Count == 0) return ValidationOutcome.Passed;
        return new ValidationOutcome(errors);
    }
}
