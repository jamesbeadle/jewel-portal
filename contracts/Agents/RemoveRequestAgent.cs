using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Agents;

// Remove an applied agent from a request (drops its watch-queue row).
public sealed record RemoveRequestAgent(string RequestId, string RequestAgentId) : ICommand<Acknowledgement>;
