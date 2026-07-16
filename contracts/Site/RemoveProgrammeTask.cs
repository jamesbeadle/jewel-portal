using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Site;

public sealed record RemoveProgrammeTask(string ProgrammeTaskId) : ICommand<Acknowledgement>;
