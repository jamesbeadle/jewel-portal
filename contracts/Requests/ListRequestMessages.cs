using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

public sealed record ListRequestMessages(string RequestId) : IQuery<IReadOnlyList<RequestMessage>>;
