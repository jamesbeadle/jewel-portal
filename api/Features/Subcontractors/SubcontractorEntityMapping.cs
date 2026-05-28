using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Subcontractors;

internal static class SubcontractorEntityMapping
{
    public static Subcontractor ToModel(this SubcontractorEntity entity) =>
        new(entity.SubcontractorId, entity.CompanyName, entity.PrimaryTrade, entity.ContactName, entity.ContactEmail, entity.ContactPhone, entity.CisStatus, entity.OnboardedAt);

    public static ComplianceDocument ToModel(this ComplianceDocumentEntity entity) =>
        new(entity.ComplianceDocumentId, entity.SubcontractorId, entity.Kind, entity.FileName, entity.ExpiresAt, entity.UploadedAt);
}
