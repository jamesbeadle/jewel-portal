using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Projects;

// Add or update an external contact on a project. A null/blank ContactId inserts a new contact;
// a populated ContactId updates the matching one in place (so editing a recipient is idempotent).
public sealed record UpsertProjectContact(
    string ProjectId,
    string Name,
    string Email,
    ProjectContactRole Role,
    bool ReceivesRequests,
    string? Organisation = null,
    string? ContactId = null) : ICommand<ProjectContact>;
