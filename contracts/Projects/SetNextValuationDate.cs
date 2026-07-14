using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Projects;

// Sets (or clears, with null) the date the next valuation is expected on a project. Kept as its
// own command — the project view edits this one field from a small date-maths modal, without
// round-tripping the full UpdateProjectDetails payload.
public sealed record SetNextValuationDate(
    string ProjectId,
    DateTimeOffset? NextExpectedValuationDate) : ICommand<Project>;
