using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// Links a request to a client account. The client's architect email is used when the request is
/// promoted to an RFI. Pass a null/empty ClientId to unlink.
/// </summary>
public sealed record LinkRequestToClient(string RequestId, string? ClientId) : ICommand<Request>;
