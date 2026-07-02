using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// Promotes a request to an RFI and issues the official RFI document to the request's linked party
/// (an architect's contact email, or a client's primary contact email when Jewel works with the
/// client directly). Resolution falls back to the project's party, then to the project's Architect
/// contact. A General request is the default state; this is the first rung of the ladder
/// General -> RFI -> (RFQ).
/// </summary>
public sealed record PromoteRequestToRfi(string RequestId) : ICommand<Request>;
