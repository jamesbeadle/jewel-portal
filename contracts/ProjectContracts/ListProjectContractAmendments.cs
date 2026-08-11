using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ProjectContracts;

/// <summary>
/// The project's contract amendments in the order they were made — amendment date first, upload
/// date as the tiebreaker — so the list reads as the history of the bargain. Empty when none have
/// been recorded, which is the common case.
/// </summary>
public sealed record ListProjectContractAmendments(string ProjectId)
    : IQuery<IReadOnlyList<ProjectContractAmendment>>;
