using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Forms.Links;
using Jewel.JPMS.Api.Features.Forms.Mail;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Forms.Retention;

public sealed partial class FormRenewalChase
{
    private async Task<int> ChaseInsuranceAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var horizon = now.AddDays(DaysBeforeExpiry);
        var expiring = await context.ComplianceDocuments
            .Where(row => row.SupersededAt == null && row.FormCompany != null && row.ExpiresAt != null && row.ExpiresAt >= now
                && row.ExpiresAt <= horizon && row.Kind.ToLower().Contains(ComplianceInsurance.KindWord))
            .ToListAsync(cancellationToken);
        var sent = 0;
        foreach (var document in expiring.Where(document => IsDue(document.LastChasedAt, document.ChaseCount, now)))
            sent += await ChaseInsuranceAsync(document, now, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return sent;
    }

    private async Task<int> ChaseInsuranceAsync(ComplianceDocumentEntity document, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var company = await context.Subcontractors.AsNoTracking()
            .FirstOrDefaultAsync(row => row.SubcontractorId == document.SubcontractorId, cancellationToken);
        var canBeReached = company is not null && FormInviteRows.IsAnEmailAddress(company.ContactEmail);
        if (!canBeReached) return 0;
        var expiresAt = document.ExpiresAt!.Value;
        var expiresOn = DateOnly.FromDateTime(expiresAt.UtcDateTime);
        var recipient = new FormLinkRecipient((JewelCompany)document.FormCompany!.Value, company!.ContactName, company.CompanyName, company.ContactEmail);
        var issued = NewInvite(FormSlugs.InsuranceUpdate, recipient, $"Renewal of {document.Kind}", now);
        var link = await SendAsync(issued, url => FormRenewalEmails.ForInsurance(
            recipient.Company, recipient.PersonName, recipient.Email, document.Kind, expiresOn, url, issued.Invite.ExpiresAt), cancellationToken);
        if (link is null) return 0;
        document.LastChasedAt = now;
        document.ChaseCount += 1;
        return 1;
    }

    private async Task<int> ChaseTrainingAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(FormClock.InLondon(now).DateTime);
        var horizon = today.AddDays(DaysBeforeExpiry);
        var expiring = await context.TrainingRecords
            .Where(row => row.EndedOn == null && row.ExpiresOn != null && row.ExpiresOn >= today && row.ExpiresOn <= horizon && row.Email != "")
            .ToListAsync(cancellationToken);
        var sent = 0;
        foreach (var record in expiring.Where(record => IsDue(record.LastChasedAt, record.ChaseCount, now)))
            sent += await ChaseTicketAsync(record, now, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return sent;
    }

    private async Task<int> ChaseTicketAsync(TrainingRecordEntity record, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var recipient = new FormLinkRecipient((JewelCompany)record.Company, record.PersonName, "", record.Email);
        var issued = NewInvite(FormSlugs.TrainingCertificate, recipient, $"Renewal of {record.Course}", now);
        var link = await SendAsync(issued, url => FormRenewalEmails.ForTraining(
            recipient.Company, record.PersonName, record.Email, record.Course, record.ExpiresOn!.Value, url, issued.Invite.ExpiresAt), cancellationToken);
        if (link is null) return 0;
        record.LastChasedAt = now;
        record.ChaseCount += 1;
        return 1;
    }
}
