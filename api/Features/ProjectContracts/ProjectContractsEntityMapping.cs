using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Api.Features.ProjectContracts;

internal static class ProjectContractsEntityMapping
{
    public static ProjectContract ToModel(this ProjectContractEntity entity) => new(
        entity.ProjectContractId,
        entity.ProjectId,
        (ContractForm)entity.Form,
        entity.FormEdition,
        entity.BespokeDeviations,
        entity.EmployerName,
        entity.ContractAdministratorName,
        entity.ContractAdministratorEmail,
        entity.ArchitectName,
        entity.ArchitectEmail,
        entity.ContractorName,
        entity.ContractSum,
        entity.LiquidatedDamagesPerWeek,
        entity.ContractDate,
        entity.PossessionDate,
        entity.CompletionDate,
        entity.RetentionPercent,
        entity.RetentionPercentAfterCompletion,
        entity.DefectsLiabilityPeriodMonths,
        entity.ApplicationCutOffDayOfMonth,
        entity.PaymentNoticeDays,
        entity.PayLessNoticeDays,
        entity.FinalDateForPaymentDays,
        entity.OhpDirectWorksPercent,
        entity.OhpSubcontractorPercent,
        entity.AttendanceOnClientDirectPercent,
        entity.DayworkLabourPercent,
        entity.DayworkMaterialsPercent,
        entity.DayworkPlantPercent,
        entity.DocumentFileName,
        entity.DocumentContentType,
        entity.DocumentFileSizeBytes,
        entity.DocumentUploadedAt,
        entity.DocumentUploadedByEmail,
        entity.UpdatedByEmail,
        entity.UpdatedAt);

    public static ProjectContractAmendment ToModel(this ProjectContractAmendmentEntity entity) => new(
        entity.ProjectContractAmendmentId,
        entity.ProjectId,
        entity.Title,
        entity.AmendmentDate,
        entity.Notes,
        entity.DocumentFileName,
        entity.DocumentContentType,
        entity.DocumentFileSizeBytes,
        entity.DocumentUploadedAt,
        entity.DocumentUploadedByEmail,
        entity.UpdatedByEmail,
        entity.UpdatedAt);
}
