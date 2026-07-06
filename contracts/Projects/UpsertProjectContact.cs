using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Projects;

// Add or update a contact on a project's correspondence profile. A null/blank ContactId inserts a
// new contact; a populated ContactId updates the matching one in place (so editing a recipient is
// idempotent). Routing is how the person joins issued request documents (To/Cc/Bcc/None); when
// omitted it is derived from the legacy ReceivesRequests flag (true → To) so older callers keep
// working. PartyContactId links the row to a person on the corresponding party's contact book,
// making this row a per-project routing override (name/email read through from the party contact).
public sealed record UpsertProjectContact(
    string ProjectId,
    string Name,
    string Email,
    ProjectContactRole Role,
    bool ReceivesRequests,
    string? Organisation = null,
    string? ContactId = null,
    CorrespondenceRouting? Routing = null,
    string? PartyContactId = null) : ICommand<ProjectContact>;
