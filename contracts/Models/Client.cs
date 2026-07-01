namespace Jewel.JPMS.Models;

// A global client account. Distinct from a project's per-project contacts: one client account can
// own many projects, and it is the canonical home of the architect email an RFI is issued to when a
// request is promoted. Resolution order for the RFI recipient is Client.ArchitectEmail first, then
// the project's Architect ProjectContact as a fallback.
public sealed record Client(
    string ClientId,
    string Name,
    string? PrimaryContactName,
    string? PrimaryContactEmail,
    string? ArchitectName,
    string? ArchitectEmail,
    DateTimeOffset CreatedAt);
