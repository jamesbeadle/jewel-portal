using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.Subcontractors;

internal static class SubcontractorEntityMapping
{
    public static Subcontractor ToModel(this SubcontractorEntity entity, IReadOnlyList<Trade> trades) =>
        new(entity.SubcontractorId, entity.CompanyName, trades, entity.ContactName, entity.ContactEmail, entity.ContactPhone, entity.CisStatus, entity.OnboardedAt,
            (DirectoryCategory)entity.Category, entity.MobileNumber, entity.Town, entity.County, entity.Website, entity.Pli, entity.PliExpiry);

    public static Trade ToModel(this TradeEntity entity) => new(entity.TradeId, entity.Name);

    public static ComplianceDocument ToModel(this ComplianceDocumentEntity entity) =>
        new(entity.ComplianceDocumentId, entity.SubcontractorId, entity.Kind, entity.FileName, entity.ExpiresAt, entity.UploadedAt);
}
