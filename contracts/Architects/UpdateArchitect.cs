using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Architects;

/// <summary>
/// Updates an architect practice's details — the name and the contact an RFI is issued to when a
/// request whose party is this architect is promoted.
/// </summary>
public sealed record UpdateArchitect(
    string ArchitectId,
    string Name,
    string? ContactName = null,
    string? ContactEmail = null) : ICommand<Architect>;
