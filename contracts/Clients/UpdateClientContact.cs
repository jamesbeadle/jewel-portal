using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Clients;

/// <summary>
/// Updates a client account's name and primary contact — the address request documents are issued
/// to when this client is the selected party on a project/request.
/// </summary>
public sealed record UpdateClientContact(
    string ClientId,
    string Name,
    string? PrimaryContactName = null,
    string? PrimaryContactEmail = null) : ICommand<Client>;
