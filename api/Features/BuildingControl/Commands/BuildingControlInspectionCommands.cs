using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.BuildingControl;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.BuildingControl.Commands;

// The inspection register's write side: add a stage, edit it, walk its status ladder, and remove
// a stage that never happened. One file for the sibling slices, as with the case commands.

public sealed class AddBuildingControlInspectionHandler : ICommandHandler<AddBuildingControlInspection, BuildingControlInspection>
{
    private readonly JpmsContext context;
    private readonly BuildingControlInspectionRegister register;

    public AddBuildingControlInspectionHandler(JpmsContext context, BuildingControlInspectionRegister register)
    {
        this.context = context;
        this.register = register;
    }

    public async Task<BuildingControlInspection> HandleAsync(AddBuildingControlInspection command, CancellationToken cancellationToken)
    {
        var buildingControlCase = await context.BuildingControlCases.AsNoTracking().FirstOrDefaultAsync(
                row => row.BuildingControlCaseId == command.BuildingControlCaseId, cancellationToken)
            ?? throw new InvalidOperationException("That building control case no longer exists.");

        var entity = await register.RaiseAsync(buildingControlCase, command.Details, command.RaisedByEmail, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

public sealed class AddBuildingControlInspectionAuthorisation
{
    public bool Allows(SignedInUser user, AddBuildingControlInspection command) =>
        BuildingControlRoles.Managers.IncludesAny(user.Roles);
}

public sealed class AddBuildingControlInspectionValidation
{
    public ValidationOutcome Check(AddBuildingControlInspection command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.BuildingControlCaseId)) errors.Add("BuildingControlCaseId is required.");
        errors.AddRange(BuildingControlRules.InspectionProblems(command.Details));
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class UpdateBuildingControlInspectionHandler : ICommandHandler<UpdateBuildingControlInspection, BuildingControlInspection>
{
    private readonly JpmsContext context;
    public UpdateBuildingControlInspectionHandler(JpmsContext context) { this.context = context; }

    public async Task<BuildingControlInspection> HandleAsync(UpdateBuildingControlInspection command, CancellationToken cancellationToken)
    {
        var entity = await context.BuildingControlInspections.FirstOrDefaultAsync(
                row => row.BuildingControlInspectionId == command.BuildingControlInspectionId, cancellationToken)
            ?? throw new InvalidOperationException("That inspection no longer exists.");

        BuildingControlRules.Apply(entity, command.Details);
        // A Planned stage that has just been given its date is Booked now; a Booked stage whose
        // date was cleared (the visit fell through) is back to Planned. Later statuses are the
        // status buttons' business — an edit never rewinds an inspected stage.
        if (entity.Status == (int)BuildingControlInspectionStatus.Planned && entity.BookedFor is not null)
            entity.Status = (int)BuildingControlInspectionStatus.Booked;
        else if (entity.Status == (int)BuildingControlInspectionStatus.Booked && entity.BookedFor is null)
            entity.Status = (int)BuildingControlInspectionStatus.Planned;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

public sealed class UpdateBuildingControlInspectionAuthorisation
{
    public bool Allows(SignedInUser user, UpdateBuildingControlInspection command) =>
        BuildingControlRoles.Managers.IncludesAny(user.Roles);
}

public sealed class UpdateBuildingControlInspectionValidation
{
    public ValidationOutcome Check(UpdateBuildingControlInspection command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.BuildingControlInspectionId)) errors.Add("BuildingControlInspectionId is required.");
        errors.AddRange(BuildingControlRules.InspectionProblems(command.Details));
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class SetBuildingControlInspectionStatusHandler : ICommandHandler<SetBuildingControlInspectionStatus, BuildingControlInspection>
{
    private readonly JpmsContext context;
    public SetBuildingControlInspectionStatusHandler(JpmsContext context) { this.context = context; }

    public async Task<BuildingControlInspection> HandleAsync(SetBuildingControlInspectionStatus command, CancellationToken cancellationToken)
    {
        var entity = await context.BuildingControlInspections.FirstOrDefaultAsync(
                row => row.BuildingControlInspectionId == command.BuildingControlInspectionId, cancellationToken)
            ?? throw new InvalidOperationException("That inspection no longer exists.");
        BuildingControlRules.ApplyStatus(entity, command.Status);
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

public sealed class SetBuildingControlInspectionStatusAuthorisation
{
    public bool Allows(SignedInUser user, SetBuildingControlInspectionStatus command) =>
        BuildingControlRoles.Managers.IncludesAny(user.Roles);
}

public sealed class DeleteBuildingControlInspectionHandler : ICommandHandler<DeleteBuildingControlInspection, Acknowledgement>
{
    private readonly JpmsContext context;
    public DeleteBuildingControlInspectionHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(DeleteBuildingControlInspection command, CancellationToken cancellationToken)
    {
        var entity = await context.BuildingControlInspections.FirstOrDefaultAsync(
                row => row.BuildingControlInspectionId == command.BuildingControlInspectionId, cancellationToken)
            ?? throw new InvalidOperationException("That inspection no longer exists.");

        // Only a stage that never happened may go: Planned, with no files. Anything booked,
        // inspected or carrying evidence is the project's history — close it instead. (Its BCI
        // number is never re-issued; the sequence is max+1 over all rows ever minted.)
        if (entity.Status != (int)BuildingControlInspectionStatus.Planned)
            throw new InvalidOperationException("Only a Planned stage can be removed — close the inspection instead.");
        var hasFiles = await context.BuildingControlAttachments.AnyAsync(
            row => row.BuildingControlInspectionId == command.BuildingControlInspectionId, cancellationToken);
        if (hasFiles)
            throw new InvalidOperationException("This stage holds files — remove them first, or close the inspection instead.");

        context.BuildingControlInspections.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return new Acknowledgement(command.BuildingControlInspectionId);
    }
}

public sealed class DeleteBuildingControlInspectionAuthorisation
{
    public bool Allows(SignedInUser user, DeleteBuildingControlInspection command) =>
        BuildingControlRoles.Managers.IncludesAny(user.Roles);
}
