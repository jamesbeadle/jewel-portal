using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Site;

public sealed record RemoveProgrammeTaskLink(string ProgrammeTaskLinkId) : ICommand<Acknowledgement>;
