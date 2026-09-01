using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Gates;
using Jewel.JPMS.Contracts.BuildingControl;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.BuildingControl.Commands;

// The case's write side: set up, edit, move along the ladder. One file for the three related
// slices — the TenderEnquiryAttachmentHandlers arrangement for small sibling handlers.

public sealed class CreateBuildingControlCaseHandler : ICommandHandler<CreateBuildingControlCase, BuildingControlCase>
{
    private readonly JpmsContext context;
    public CreateBuildingControlCaseHandler(JpmsContext context) { this.context = context; }

    public async Task<BuildingControlCase> HandleAsync(CreateBuildingControlCase command, CancellationToken cancellationToken)
    {
        var projectExists = await context.Projects.AnyAsync(p => p.ProjectId == command.ProjectId, cancellationToken);
        if (!projectExists) throw new InvalidOperationException($"Project '{command.ProjectId}' not found.");

        // One ACTIVE case per project — a lapsed or completion-certified case may be succeeded,
        // a working one may not: two live notices for one job is a data error, not a feature.
        var existing = await context.BuildingControlCases.AsNoTracking()
            .Where(row => row.ProjectId == command.ProjectId)
            .ToListAsync(cancellationToken);
        if (existing.Any(BuildingControlRules.IsActive))
            throw new InvalidOperationException(
                "This project already has an active building control case. Mark it Lapsed (or certify completion) before setting up a successor.");

        // Global sequence (the defect rule): max + 1, never a row count — deleted rows must not
        // re-issue a number, because the number is the mailbox tag stem ("JPMS/BC-0001").
        var nextNumber = (await context.BuildingControlCases.MaxAsync(row => (int?)row.Number, cancellationToken) ?? 0) + 1;

        var entity = new BuildingControlCaseEntity
        {
            BuildingControlCaseId = BuildingControlIdentifierFactory.Next(),
            ProjectId = command.ProjectId,
            Number = nextNumber,
            Status = (int)BuildingControlCaseStatus.NoticeSubmitted,
            CreatedByEmail = command.CreatedByEmail,
            CreatedAt = DateTimeOffset.UtcNow
        };
        BuildingControlRules.Apply(entity, command.Details);
        // A case whose acceptance date is already known arrives In force — the common backfill
        // shape (the notice went in months ago; the tab is being set up today).
        if (entity.AcceptedOn is not null) entity.Status = (int)BuildingControlCaseStatus.InForce;
        context.BuildingControlCases.Add(entity);

        if (command.SeedStandardStages)
        {
            // The default checklist, planted as Planned stages in running order — a starting
            // point, freely renamed/reordered/deleted (BuildingControlStages.DefaultChecklist).
            var nextInspectionNumber =
                (await context.BuildingControlInspections.MaxAsync(row => (int?)row.Number, cancellationToken) ?? 0) + 1;
            var order = 1;
            foreach (var stage in BuildingControlStages.DefaultChecklist)
            {
                context.BuildingControlInspections.Add(new BuildingControlInspectionEntity
                {
                    BuildingControlInspectionId = BuildingControlIdentifierFactory.Next(),
                    BuildingControlCaseId = entity.BuildingControlCaseId,
                    ProjectId = command.ProjectId,
                    Number = nextInspectionNumber++,
                    StageName = stage,
                    Status = (int)BuildingControlInspectionStatus.Planned,
                    DisplayOrder = order++,
                    RaisedByEmail = command.CreatedByEmail,
                    RaisedAt = DateTimeOffset.UtcNow
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

public sealed class CreateBuildingControlCaseAuthorisation
{
    public bool Allows(SignedInUser user, CreateBuildingControlCase command) =>
        BuildingControlRoles.Managers.IncludesAny(user.Roles);
}

public sealed class CreateBuildingControlCaseValidation
{
    public ValidationOutcome Check(CreateBuildingControlCase command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.ProjectId)) errors.Add("ProjectId is required.");
        errors.AddRange(BuildingControlRules.CaseProblems(command.Details));
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class UpdateBuildingControlCaseHandler : ICommandHandler<UpdateBuildingControlCase, BuildingControlCase>
{
    private readonly JpmsContext context;
    public UpdateBuildingControlCaseHandler(JpmsContext context) { this.context = context; }

    public async Task<BuildingControlCase> HandleAsync(UpdateBuildingControlCase command, CancellationToken cancellationToken)
    {
        var entity = await context.BuildingControlCases.FirstOrDefaultAsync(
                row => row.BuildingControlCaseId == command.BuildingControlCaseId, cancellationToken)
            ?? throw new InvalidOperationException("That building control case no longer exists.");
        BuildingControlRules.Apply(entity, command.Details);
        // A NoticeSubmitted case whose acceptance date has just been entered is In force now.
        if (entity.Status == (int)BuildingControlCaseStatus.NoticeSubmitted && entity.AcceptedOn is not null)
            entity.Status = (int)BuildingControlCaseStatus.InForce;
        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

public sealed class UpdateBuildingControlCaseAuthorisation
{
    public bool Allows(SignedInUser user, UpdateBuildingControlCase command) =>
        BuildingControlRoles.Managers.IncludesAny(user.Roles);
}

public sealed class UpdateBuildingControlCaseValidation
{
    public ValidationOutcome Check(UpdateBuildingControlCase command)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(command.BuildingControlCaseId)) errors.Add("BuildingControlCaseId is required.");
        errors.AddRange(BuildingControlRules.CaseProblems(command.Details));
        return errors.Count == 0 ? ValidationOutcome.Passed : new ValidationOutcome(errors);
    }
}

public sealed class SetBuildingControlCaseStatusHandler : ICommandHandler<SetBuildingControlCaseStatus, BuildingControlCase>
{
    private readonly JpmsContext context;
    public SetBuildingControlCaseStatusHandler(JpmsContext context) { this.context = context; }

    public async Task<BuildingControlCase> HandleAsync(SetBuildingControlCaseStatus command, CancellationToken cancellationToken)
    {
        var entity = await context.BuildingControlCases.FirstOrDefaultAsync(
                row => row.BuildingControlCaseId == command.BuildingControlCaseId, cancellationToken)
            ?? throw new InvalidOperationException("That building control case no longer exists.");

        entity.Status = (int)command.Status;
        // The certificate date travels with the certified status: stamped on the way in (today
        // unless the caller names the official date), cleared on the way out.
        entity.CompletionCertifiedOn = command.Status == BuildingControlCaseStatus.CompletionCertified
            ? BuildingControlRules.AsCalendarDate(command.CompletionCertifiedOn ?? DateTimeOffset.UtcNow)
            : null;

        await context.SaveChangesAsync(cancellationToken);
        return entity.ToModel();
    }
}

public sealed class SetBuildingControlCaseStatusAuthorisation
{
    public bool Allows(SignedInUser user, SetBuildingControlCaseStatus command) =>
        BuildingControlRoles.Managers.IncludesAny(user.Roles);
}
