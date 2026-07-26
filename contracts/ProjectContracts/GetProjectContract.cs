using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.ProjectContracts;

/// <summary>The contract for a project, or null when none has been recorded yet.</summary>
public sealed record GetProjectContract(string ProjectId) : IQuery<ProjectContract?>;
