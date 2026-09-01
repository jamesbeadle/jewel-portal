using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Contracts.BuildingControl;

namespace Jewel.JPMS.Api.Features.BuildingControl.Commands;

/// <summary>
/// Writes the inspection row itself — numbered on the global BCI sequence, at the foot of the
/// case's running order, stamped with who raised it. Shared by the tab's Add inspection and the
/// triage create-from-message so both mint the same way (the CalendarEventRegister pattern).
/// Callers save the context.
/// </summary>
public sealed class BuildingControlInspectionRegister
{
    private readonly JpmsContext context;

    public BuildingControlInspectionRegister(JpmsContext context) { this.context = context; }

    public async Task<BuildingControlInspectionEntity> RaiseAsync(
        BuildingControlCaseEntity buildingControlCase, BuildingControlInspectionDetails details,
        string raisedByEmail, CancellationToken cancellationToken)
    {
        var entity = new BuildingControlInspectionEntity
        {
            BuildingControlInspectionId = BuildingControlIdentifierFactory.Next(),
            BuildingControlCaseId = buildingControlCase.BuildingControlCaseId,
            ProjectId = buildingControlCase.ProjectId,
            Number = await NextNumberAsync(cancellationToken),
            Status = (int)BuildingControlRules.StatusOnAdd(details),
            DisplayOrder = await NextDisplayOrderAsync(buildingControlCase.BuildingControlCaseId, cancellationToken),
            RaisedByEmail = raisedByEmail,
            RaisedAt = DateTimeOffset.UtcNow
        };
        BuildingControlRules.Apply(entity, details);
        context.BuildingControlInspections.Add(entity);
        return entity;
    }

    // Global sequence: max + 1, never a row count — deleted rows must not re-issue a number,
    // because the number is the mailbox tag stem ("JPMS/BCI-0001").
    private async Task<int> NextNumberAsync(CancellationToken cancellationToken) =>
        (await context.BuildingControlInspections.MaxAsync(row => (int?)row.Number, cancellationToken) ?? 0) + 1;

    private async Task<int> NextDisplayOrderAsync(string caseId, CancellationToken cancellationToken) =>
        (await context.BuildingControlInspections
            .Where(row => row.BuildingControlCaseId == caseId)
            .MaxAsync(row => (int?)row.DisplayOrder, cancellationToken) ?? 0) + 1;
}
