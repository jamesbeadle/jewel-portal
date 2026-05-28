using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Projects;

public sealed record UpdateProjectDetails(
    string ProjectId,
    string Reference,
    string Name,
    string ClientName,
    Organisation Organisation,
    ProjectStage Stage,
    string ProjectManagerEmail) : ICommand<Project>;
