using Jewel.JPMS.Api.Features.Forms.Mapping;
using Jewel.JPMS.Contracts.Forms;

namespace Jewel.JPMS.Api.Features.Forms.Registers;

/// <summary>The right-to-work register, by name — read only by the people the restricted store admits.</summary>
public sealed class ListRightToWorkChecksHandler : IQueryHandler<ListRightToWorkChecks, IReadOnlyList<RightToWorkCheck>>
{
    private readonly JpmsContext context;

    public ListRightToWorkChecksHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<RightToWorkCheck>> HandleAsync(ListRightToWorkChecks query, CancellationToken cancellationToken)
    {
        var checks = await context.RightToWorkChecks.AsNoTracking().OrderBy(row => row.PersonName).ToListAsync(cancellationToken);
        var evidenceIds = checks.Where(check => check.EvidenceUploadId != null).Select(check => check.EvidenceUploadId!).ToList();
        var names = await context.FormUploads.AsNoTracking()
            .Where(row => evidenceIds.Contains(row.FormUploadId))
            .ToDictionaryAsync(row => row.FormUploadId, row => row.FileName, cancellationToken);
        return checks.Select(check => check.ToModel(names.GetValueOrDefault(check.EvidenceUploadId ?? "", ""))).ToList();
    }
}

/// <summary>The training register, soonest expiry first — the certificates with no expiry last, never chased.</summary>
public sealed class ListTrainingRecordsHandler : IQueryHandler<ListTrainingRecords, IReadOnlyList<TrainingRecord>>
{
    private readonly JpmsContext context;

    public ListTrainingRecordsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<TrainingRecord>> HandleAsync(ListTrainingRecords query, CancellationToken cancellationToken)
    {
        var records = await context.TrainingRecords.AsNoTracking().ToListAsync(cancellationToken);
        return records.Select(record => record.ToModel())
            .OrderBy(record => record.ExpiresOn ?? DateOnly.MaxValue).ThenBy(record => record.PersonName).ToList();
    }
}

/// <summary>Every workstation action, the open ones first — the work queue the noes make.</summary>
public sealed class ListWorkstationActionsHandler : IQueryHandler<ListWorkstationActions, IReadOnlyList<WorkstationAction>>
{
    private readonly JpmsContext context;

    public ListWorkstationActionsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<WorkstationAction>> HandleAsync(ListWorkstationActions query, CancellationToken cancellationToken)
    {
        var actions = await context.WorkstationActions.AsNoTracking()
            .OrderBy(row => row.State == (int)WorkstationActionState.Open ? 0 : 1).ThenByDescending(row => row.RaisedAt)
            .ToListAsync(cancellationToken);
        return actions.Select(action => action.ToModel()).ToList();
    }
}

/// <summary>The licence checks, newest first: the outcome, never the offence.</summary>
public sealed class ListDrivingLicenceChecksHandler : IQueryHandler<ListDrivingLicenceChecks, IReadOnlyList<DrivingLicenceCheck>>
{
    private readonly JpmsContext context;

    public ListDrivingLicenceChecksHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<DrivingLicenceCheck>> HandleAsync(ListDrivingLicenceChecks query, CancellationToken cancellationToken)
    {
        var checks = await context.DrivingLicenceChecks.AsNoTracking().OrderByDescending(row => row.CheckedAt).ToListAsync(cancellationToken);
        return checks.Select(check => check.ToModel()).ToList();
    }
}
