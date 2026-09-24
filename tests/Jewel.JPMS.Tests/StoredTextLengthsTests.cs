using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// 2026-09-24, the field-length audit: a form's maxlength is StoredTextLengths, and every figure
// there must be the column it stops the typing for — or the form lets through what the save refuses.
public sealed class StoredTextLengthsTests
{
    public static TheoryData<int, Type, string> Pins => new()
    {
        { StoredTextLengths.Name, typeof(ArchitectEntity), nameof(ArchitectEntity.Name) },
        { StoredTextLengths.Name, typeof(ArchitectEntity), nameof(ArchitectEntity.ContactName) },
        { StoredTextLengths.Name, typeof(ClientEntity), nameof(ClientEntity.Name) },
        { StoredTextLengths.Name, typeof(ClientEntity), nameof(ClientEntity.PrimaryContactName) },
        { StoredTextLengths.Name, typeof(PartyContactEntity), nameof(PartyContactEntity.Name) },
        { StoredTextLengths.Name, typeof(ProjectContactEntity), nameof(ProjectContactEntity.Name) },
        { StoredTextLengths.Name, typeof(ProjectContactEntity), nameof(ProjectContactEntity.Organisation) },
        { StoredTextLengths.Name, typeof(SubcontractorEntity), nameof(SubcontractorEntity.CompanyName) },
        { StoredTextLengths.Name, typeof(SubcontractorEntity), nameof(SubcontractorEntity.ContactName) },
        { StoredTextLengths.Name, typeof(CompanyContactEntity), nameof(CompanyContactEntity.Name) },
        { StoredTextLengths.Name, typeof(FormPackEntity), nameof(FormPackEntity.PersonName) },
        { StoredTextLengths.Name, typeof(RightToWorkCheckEntity), nameof(RightToWorkCheckEntity.PersonName) },
        { StoredTextLengths.Name, typeof(RightToWorkCheckEntity), nameof(RightToWorkCheckEntity.CheckedByName) },
        { StoredTextLengths.Name, typeof(HsAuditEntity), nameof(HsAuditEntity.ManagerName) },
        { StoredTextLengths.Name, typeof(HsRecordEntity), nameof(HsRecordEntity.AssignedToName) },
        { StoredTextLengths.Name, typeof(HsAuditItemEntity), nameof(HsAuditItemEntity.OwnerName) },
        { StoredTextLengths.Name, typeof(ProjectContractEntity), nameof(ProjectContractEntity.EmployerName) },
        { StoredTextLengths.Name, typeof(ProjectContractEntity), nameof(ProjectContractEntity.ContractAdministratorName) },
        { StoredTextLengths.Name, typeof(ProjectContractEntity), nameof(ProjectContractEntity.ArchitectName) },
        { StoredTextLengths.Name, typeof(ProjectContractEntity), nameof(ProjectContractEntity.ContractorName) },
        { StoredTextLengths.Name, typeof(ProjectEntity), nameof(ProjectEntity.Name) },
        { StoredTextLengths.Name, typeof(ProjectEntity), nameof(ProjectEntity.ClientName) },
        { StoredTextLengths.Name, typeof(ProjectEntity), nameof(ProjectEntity.SiteManagerName) },
        { StoredTextLengths.Name, typeof(PolicySignOffEntity), nameof(PolicySignOffEntity.SignedName) },
        { StoredTextLengths.Name, typeof(CompanyRegisterItemEntity), nameof(CompanyRegisterItemEntity.Name) },
        { StoredTextLengths.Name, typeof(CompanyRegisterItemEntity), nameof(CompanyRegisterItemEntity.Counterparty) },
        { StoredTextLengths.Title, typeof(RequestEntity), nameof(RequestEntity.Title) },
        { StoredTextLengths.Title, typeof(ProgrammeTaskEntity), nameof(ProgrammeTaskEntity.Title) },
        { StoredTextLengths.Title, typeof(ProgressReportEntity), nameof(ProgressReportEntity.Title) },
        { StoredTextLengths.Title, typeof(PolicyDocumentEntity), nameof(PolicyDocumentEntity.Title) },
        { StoredTextLengths.Title, typeof(ValuationLineItemEntity), nameof(ValuationLineItemEntity.VariationTitle) },
        { StoredTextLengths.Title, typeof(PartyContactEntity), nameof(PartyContactEntity.JobTitle) },
        { StoredTextLengths.AddressLine, typeof(ProjectEntity), nameof(ProjectEntity.AddressLine) },
        { StoredTextLengths.AddressLine, typeof(SubcontractorEntity), nameof(SubcontractorEntity.AddressLine) },
        { StoredTextLengths.Town, typeof(ProjectEntity), nameof(ProjectEntity.Town) },
        { StoredTextLengths.Town, typeof(SubcontractorEntity), nameof(SubcontractorEntity.Town) },
        { StoredTextLengths.County, typeof(SubcontractorEntity), nameof(SubcontractorEntity.County) },
        { StoredTextLengths.Website, typeof(SubcontractorEntity), nameof(SubcontractorEntity.Website) },
        { StoredTextLengths.Reference, typeof(ProjectEntity), nameof(ProjectEntity.Reference) },
        { StoredTextLengths.Reference, typeof(RequestEntity), nameof(RequestEntity.Reference) },
        { StoredTextLengths.Reference, typeof(RightToWorkCheckEntity), nameof(RightToWorkCheckEntity.Reference) },
        { StoredTextLengths.RegisterReference, typeof(CompanyRegisterItemEntity), nameof(CompanyRegisterItemEntity.Reference) },
        { StoredTextLengths.DocumentReference, typeof(RightToWorkCheckEntity), nameof(RightToWorkCheckEntity.DocumentReference) },
        { StoredTextLengths.ProviderName, typeof(RightToWorkCheckEntity), nameof(RightToWorkCheckEntity.IdspProvider) },
        { StoredTextLengths.Code, typeof(ValuationLineItemEntity), nameof(ValuationLineItemEntity.VariationRef) },
        { StoredTextLengths.Code, typeof(ValuationLineItemEntity), nameof(ValuationLineItemEntity.SectionCode) },
        { StoredTextLengths.Unit, typeof(ValuationLineItemEntity), nameof(ValuationLineItemEntity.Unit) },
        { StoredTextLengths.SectionName, typeof(ValuationLineItemEntity), nameof(ValuationLineItemEntity.SectionName) },
        { StoredTextLengths.PeriodName, typeof(ValuationClaimEntity), nameof(ValuationClaimEntity.Name) },
        { StoredTextLengths.GroupName, typeof(CostCentreGroupEntity), nameof(CostCentreGroupEntity.Name) },
        { StoredTextLengths.PackageName, typeof(ReconciliationPackageEntity), nameof(ReconciliationPackageEntity.Name) },
        { StoredTextLengths.Trade, typeof(BidPackageLineItemEntity), nameof(BidPackageLineItemEntity.Trade) },
        { StoredTextLengths.ContractEdition, typeof(ProjectContractEntity), nameof(ProjectContractEntity.FormEdition) },
        { StoredTextLengths.XeroSiteName, typeof(ProjectEntity), nameof(ProjectEntity.XeroSiteName) },
        { StoredTextLengths.EmailSubject, typeof(BidPackageEntity), nameof(BidPackageEntity.InviteDraftSubject) },
        { StoredTextLengths.EmailRecipients, typeof(BidPackageEntity), nameof(BidPackageEntity.InviteDraftTo) },
        { StoredTextLengths.EmailRecipients, typeof(BidPackageEntity), nameof(BidPackageEntity.InviteDraftCc) },
    };

    [Theory]
    [MemberData(nameof(Pins))]
    public void EachFormLimit_isTheColumnItGuards(int formLimit, Type record, string field)
    {
        using var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
            .UseInMemoryDatabase($"stored-lengths-{Guid.NewGuid():N}").Options);
        var column = context.Model.FindEntityType(record)!.FindProperty(field)!;

        Assert.Equal(column.GetMaxLength(), formLimit);
    }
}
