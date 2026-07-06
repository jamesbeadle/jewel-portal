using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Site;

public sealed record AddProgrammeTaskLink(
    string ProjectId,
    string PredecessorTaskId,
    string SuccessorTaskId,
    int LagDays) : ICommand<ProgrammeTaskLink>;
