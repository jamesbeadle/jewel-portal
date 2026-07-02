using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

/// <summary>
/// Links a request to the party it is corresponded with — a client account directly, or an
/// architect practice acting on a client's behalf (in which case <see cref="OnBehalfOfClientId"/>
/// optionally records the underlying client). The party's email is where the official document is
/// addressed when the request is promoted to an RFI. Pass a null/empty PartyId to unlink.
/// </summary>
public sealed record LinkRequestToParty(
    string RequestId,
    PartyKind PartyKind = PartyKind.Client,
    string? PartyId = null,
    string? OnBehalfOfClientId = null) : ICommand<Request>;
