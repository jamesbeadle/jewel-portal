using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Projects;

public sealed record CreateProjectShell(
    string Reference,
    string Name,
    string ClientName,
    Organisation Organisation,
    string ProjectManagerEmail) : ICommand<Project>;
