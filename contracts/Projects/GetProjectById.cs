using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Projects;

public sealed record GetProjectById(string ProjectId) : IQuery<Project?>;
