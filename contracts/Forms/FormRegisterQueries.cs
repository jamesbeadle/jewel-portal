using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Forms;

public sealed record ListRightToWorkChecks : IQuery<IReadOnlyList<RightToWorkCheck>>;

public sealed record ListTrainingRecords : IQuery<IReadOnlyList<TrainingRecord>>;

/// <summary>Every workstation action, open first — the work queue the noes make.</summary>
public sealed record ListWorkstationActions : IQuery<IReadOnlyList<WorkstationAction>>;

public sealed record ListDrivingLicenceChecks : IQuery<IReadOnlyList<DrivingLicenceCheck>>;
