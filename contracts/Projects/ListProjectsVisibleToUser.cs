using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Projects;

public sealed record ListProjectsVisibleToUser : IQuery<IReadOnlyList<Project>>;
