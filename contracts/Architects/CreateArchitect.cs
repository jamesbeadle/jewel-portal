using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Architects;

/// <summary>
/// Creates a global architect practice. The contact email captured here is where RFIs and other
/// request documents are addressed when this architect is the selected party on a project/request.
/// </summary>
public sealed record CreateArchitect(
    string Name,
    string? ContactName = null,
    string? ContactEmail = null) : ICommand<Architect>;
