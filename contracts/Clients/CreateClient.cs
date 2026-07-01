using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Clients;

/// <summary>
/// Creates a global client account. The architect email captured here is the canonical recipient
/// for RFIs raised against requests linked to this client.
/// </summary>
public sealed record CreateClient(
    string Name,
    string? PrimaryContactName = null,
    string? PrimaryContactEmail = null,
    string? ArchitectName = null,
    string? ArchitectEmail = null) : ICommand<Client>;
