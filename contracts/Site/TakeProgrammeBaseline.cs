using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Site;

public sealed record TakeProgrammeBaseline(
    string ProjectId,
    string Label,
    string TakenByEmail) : ICommand<ProgrammeBaseline>;
