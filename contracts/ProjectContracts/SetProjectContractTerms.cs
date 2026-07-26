using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ProjectContracts;

/// <summary>
/// Records or replaces the contract terms for a project. Upsert: one contract per project, created
/// on first call. Deliberately does not touch the uploaded document — that is
/// <c>UploadProjectContractDocument</c>, so re-keying a term can never detach the executed contract.
///
/// <para><c>UpdatedByEmail</c> is re-stamped from the session by the endpoint; whatever a client
/// sends is discarded.</para>
/// </summary>
public sealed record SetProjectContractTerms(
    string ProjectId,
    string UpdatedByEmail,

    ContractForm Form,
    string? FormEdition,
    string? BespokeDeviations,

    string? EmployerName,
    string? ContractAdministratorName,
    string? ContractAdministratorEmail,
    string? ArchitectName,
    string? ArchitectEmail,
    string? ContractorName,

    decimal ContractSum,
    decimal LiquidatedDamagesPerWeek,

    DateTimeOffset? ContractDate,
    DateTimeOffset? PossessionDate,
    DateTimeOffset? CompletionDate,

    decimal RetentionPercent,
    decimal RetentionPercentAfterCompletion,
    int DefectsLiabilityPeriodMonths,

    int? ApplicationCutOffDayOfMonth,
    int PaymentNoticeDays,
    int PayLessNoticeDays,
    int FinalDateForPaymentDays,

    decimal OhpDirectWorksPercent,
    decimal OhpSubcontractorPercent,
    decimal AttendanceOnClientDirectPercent,
    decimal DayworkLabourPercent,
    decimal DayworkMaterialsPercent,
    decimal DayworkPlantPercent) : ICommand<ProjectContract>;
