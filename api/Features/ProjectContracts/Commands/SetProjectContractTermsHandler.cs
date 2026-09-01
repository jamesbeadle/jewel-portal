using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.ProjectContracts;

namespace Jewel.JPMS.Api.Features.ProjectContracts.Commands;

/// <summary>
/// Upsert of the contract terms for a project. Deliberately never touches the document columns, so
/// re-keying a term cannot detach the executed contract PDF.
/// </summary>
public sealed class SetProjectContractTermsHandler : ICommandHandler<SetProjectContractTerms, ProjectContract>
{
    private readonly JpmsContext context;

    public SetProjectContractTermsHandler(JpmsContext context)
    {
        this.context = context;
    }

    public async Task<ProjectContract> HandleAsync(SetProjectContractTerms command, CancellationToken cancellationToken)
    {
        var project = await context.Projects
            .FirstOrDefaultAsync(row => row.ProjectId == command.ProjectId, cancellationToken);
        if (project is null) throw new InvalidOperationException($"Project {command.ProjectId} not found.");

        var entity = await context.ProjectContracts
            .FirstOrDefaultAsync(row => row.ProjectId == command.ProjectId, cancellationToken);

        if (entity is null)
        {
            entity = new ProjectContractEntity
            {
                ProjectContractId = ProjectContractsIdentifierFactory.NextProjectContractId(),
                ProjectId = command.ProjectId
            };
            context.ProjectContracts.Add(entity);
        }

        entity.Form = (int)command.Form;
        entity.FormEdition = Trimmed(command.FormEdition);
        entity.BespokeDeviations = Trimmed(command.BespokeDeviations);

        entity.EmployerName = Trimmed(command.EmployerName);
        entity.ContractAdministratorName = Trimmed(command.ContractAdministratorName);
        entity.ContractAdministratorEmail = Trimmed(command.ContractAdministratorEmail);
        entity.ArchitectName = Trimmed(command.ArchitectName);
        entity.ArchitectEmail = Trimmed(command.ArchitectEmail);
        entity.ContractorName = Trimmed(command.ContractorName);

        entity.ContractSum = command.ContractSum;
        entity.LiquidatedDamagesPerWeek = command.LiquidatedDamagesPerWeek;

        entity.ContractDate = command.ContractDate;
        entity.PossessionDate = command.PossessionDate;
        entity.CompletionDate = command.CompletionDate;

        entity.RetentionPercent = command.RetentionPercent;
        entity.RetentionPercentAfterCompletion = command.RetentionPercentAfterCompletion;
        entity.DefectsLiabilityPeriodMonths = command.DefectsLiabilityPeriodMonths;

        entity.ApplicationCutOffDayOfMonth = command.ApplicationCutOffDayOfMonth;
        entity.PaymentNoticeDays = command.PaymentNoticeDays;
        entity.PayLessNoticeDays = command.PayLessNoticeDays;
        entity.FinalDateForPaymentDays = command.FinalDateForPaymentDays;

        entity.OhpDirectWorksPercent = command.OhpDirectWorksPercent;
        entity.OhpSubcontractorPercent = command.OhpSubcontractorPercent;
        entity.AttendanceOnClientDirectPercent = command.AttendanceOnClientDirectPercent;
        entity.DayworkLabourPercent = command.DayworkLabourPercent;
        entity.DayworkMaterialsPercent = command.DayworkMaterialsPercent;
        entity.DayworkPlantPercent = command.DayworkPlantPercent;

        entity.UpdatedByEmail = command.UpdatedByEmail;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }

    private static string? Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
