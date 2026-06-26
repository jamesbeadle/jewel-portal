using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Requests;

public sealed record DeleteRequest(string RequestId) : ICommand<Acknowledgement>;
