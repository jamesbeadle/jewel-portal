using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Clients;

/// <summary>
/// Creates a global client account. The primary contact email captured here is where request
/// documents are addressed when this client is the selected party on a project/request.
/// Architects are managed separately — see the Architects contracts.
/// </summary>
public sealed record CreateClient(
    string Name,
    string? PrimaryContactName = null,
    string? PrimaryContactEmail = null) : ICommand<Client>;
