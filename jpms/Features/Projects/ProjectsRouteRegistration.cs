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
    }
}
