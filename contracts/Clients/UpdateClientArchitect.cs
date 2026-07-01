using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Clients;

/// <summary>
/// Updates the architect (and primary contact) details on a client account — the address an RFI is
/// issued to when a request linked to this client is promoted.
/// </summary>
public sealed record UpdateClientArchitect(
    string ClientId,
    string? ArchitectName,
    string? ArchitectEmail,
    string? PrimaryContactName = null,
    string? PrimaryContactEmail = null) : ICommand<Client>;
