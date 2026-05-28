using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.AccessRequests;

public sealed record ListPendingAccessRequests : IQuery<IReadOnlyList<AccessRequest>>;
