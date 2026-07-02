namespace Jewel.JPMS.Models;

// A global architect practice, managed separately from client accounts. Architects typically
// communicate with clients on Jewel's behalf: a project or request either deals with a client
// directly or with an architect acting for a client (see PartyKind). The contact email here is
// where RFIs and other request documents are addressed when an architect is the selected party.
public sealed record Architect(
    string ArchitectId,
    string Name,
    string? ContactName,
    string? ContactEmail,
    DateTimeOffset CreatedAt);
