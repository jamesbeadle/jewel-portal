using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Cqrs;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Features.Projects;

public static class ProjectsRouteRegistration
{
    public static IServiceCollection AddProjectsReadModels(this IServiceCollection services)
    {
        services.AddScoped<ProjectListReadModel>();
        return services;
    }

    public static void RegisterProjectsRoutes(QueryRouteTable queries, CommandRouteTable commands)
    {
        queries.Register<ListProjectsVisibleToUser, IReadOnlyList<Project>>(
            QueryRoute.Static("/api/projects"));

        queries.Register<GetProjectById, Project?>(
            new QueryRoute(
                "/api/projects/{projectId}",
                query => $"/api/projects/{((GetProjectById)query).ProjectId}"));

        commands.Register<CreateProjectShell, Project>(
            CommandRoute.Post("/api/projects"));

        commands.Register<UpdateProjectDetails, Project>(
            new CommandRoute(
                "PUT",
                "/api/projects/{projectId}",
                command => $"/api/projects/{((UpdateProjectDetails)command).ProjectId}"));

        // The project's correspondence profile: linked party-contact routing overrides plus the
        // ad-hoc To/CC/BCC recipients (e.g. internal Jewel staff copied on issued documents).
        queries.Register<ListProjectContacts, IReadOnlyList<ProjectContact>>(
            new QueryRoute("/api/projects/{projectId}/contacts",
                query => $"/api/projects/{((ListProjectContacts)query).ProjectId}/contacts"));

        commands.Register<UpsertProjectContact, ProjectContact>(
            new CommandRoute("POST", "/api/projects/{projectId}/contacts",
                command => $"/api/projects/{((UpsertProjectContact)command).ProjectId}/contacts"));

        commands.Register<RemoveProjectContact, Acknowledgement>(
            new CommandRoute("DELETE", "/api/projects/{projectId}/contacts/{contactId}",
                command =>
                {
                    var c = (RemoveProjectContact)command;
                    return $"/api/projects/{c.ProjectId}/contacts/{c.ContactId}";
                }));
    }
}
