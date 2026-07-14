using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Features.Projects.Commands;
using Jewel.JPMS.Api.Features.Projects.Contacts;
using Jewel.JPMS.Api.Features.Projects.Queries;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Projects;

public static class ProjectsFeatureRegistration
{
    public static IServiceCollection AddProjectsFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListProjectsVisibleToUser, IReadOnlyList<Project>>, ListProjectsVisibleToUserHandler>();
        services.AddScoped<IQueryHandler<GetProjectById, Project?>, GetProjectByIdHandler>();

        services.AddScoped<ICommandHandler<CreateProjectShell, Project>, CreateProjectShellHandler>();
        services.AddScoped<CreateProjectShellAuthorisation>();
        services.AddScoped<CreateProjectShellValidation>();

        services.AddScoped<ICommandHandler<UpdateProjectDetails, Project>, UpdateProjectDetailsHandler>();
        services.AddScoped<UpdateProjectDetailsAuthorisation>();
        services.AddScoped<UpdateProjectDetailsValidation>();

        services.AddScoped<ICommandHandler<SetNextValuationDate, Project>, SetNextValuationDateHandler>();
        services.AddScoped<SetNextValuationDateAuthorisation>();

        // Project contacts — the clients/architects a project's RFIs and requests are issued to.
        services.AddScoped<IQueryHandler<ListProjectContacts, IReadOnlyList<ProjectContact>>, ListProjectContactsHandler>();
        services.AddScoped<ICommandHandler<UpsertProjectContact, ProjectContact>, UpsertProjectContactHandler>();
        services.AddScoped<ICommandHandler<RemoveProjectContact, Acknowledgement>, RemoveProjectContactHandler>();
        services.AddScoped<ProjectContactAuthorisation>();
        services.AddScoped<UpsertProjectContactValidation>();

        return services;
    }
}
