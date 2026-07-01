using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// Promotes a request to an RFI and issues the official RFI document to the architect. The architect
/// email is resolved from the request's linked client account first, falling back to the project's
/// Architect contact. A General request is the default state; this is the first rung of the ladder
/// General -> RFI -> (RFQ).
/// </summary>
public sealed record PromoteRequestToRfi(string RequestId) : ICommand<Request>;
