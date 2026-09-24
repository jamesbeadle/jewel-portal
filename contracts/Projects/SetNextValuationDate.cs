using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Projects;

// Sets (or clears, with null) the next valuation date; a null Cycle is "not supplied" and keeps
// the stored cycle. Its own command so Project settings edits it without UpdateProjectDetails.
public sealed record SetNextValuationDate(
    string ProjectId,
    DateTimeOffset? NextExpectedValuationDate,
    ValuationCycle? Cycle = null) : ICommand<Project>;
