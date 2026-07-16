using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Site;

public sealed record RemoveProgrammeBaseline(string ProgrammeBaselineId) : ICommand<Acknowledgement>;
