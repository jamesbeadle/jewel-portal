using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.AccessRequests;

public sealed record ResolveAccessRequest(string Email) : ICommand<Acknowledgement>;
