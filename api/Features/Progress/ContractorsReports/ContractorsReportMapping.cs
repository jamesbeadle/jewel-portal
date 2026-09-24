using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports;

internal static class ContractorsReportMapping
{
    public static ContractorsReport ToModel(this ContractorsReportEntity entity) => new(
        entity.ContractorsReportId,
        entity.ProjectId,
        entity.Number,
        entity.PeriodStart,
        entity.PeriodEnd,
        entity.ValuationNumber,
        entity.ProgrammeReference,
        entity.PreparedByName,
        entity.IssuedTo,
        entity.DateOfIssue,
        ContractorsReportJson.Read<ContractorsReportLookAheadItem>(entity.LookAheadJson),
        entity.Neighbours,
        entity.HealthAndSafety,
        entity.BuildingControlLiaison,
        ContractorsReportJson.Read<ContractorsReportAttendance>(entity.AttendanceJson),
        ContractorsReportJson.Read<string>(entity.SelectedUpdateIdsJson),
        entity.BuildingControlContact,
        ContractorsReportJson.Read<string>(entity.ExcludedPhotoIdsJson),
        entity.CreatedByEmail,
        entity.CreatedAt,
        entity.UpdatedAt);
}
