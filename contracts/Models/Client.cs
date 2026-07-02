namespace Jewel.JPMS.Models;

// A global client account. Distinct from a project's per-project contacts: one client account can
// own many projects. Architects are managed separately (see Architect / PartyKind) — a project or
// request either deals with this client directly, or with an architect acting on the client's
// behalf. When the client is the selected party, request documents are addressed to the primary
// contact email here.
public sealed record Client(
    string ClientId,
    string Name,
    string? PrimaryContactName,
    string? PrimaryContactEmail,
    DateTimeOffset CreatedAt);
