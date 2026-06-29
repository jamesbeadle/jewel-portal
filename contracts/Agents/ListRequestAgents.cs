using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Agents;

// The agents currently applied to a request, with their completion state.
public sealed record ListRequestAgents(string RequestId) : IQuery<IReadOnlyList<RequestAgent>>;
