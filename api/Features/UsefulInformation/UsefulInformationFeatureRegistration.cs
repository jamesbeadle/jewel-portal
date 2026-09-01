using Jewel.JPMS.Api.Features.UsefulInformation.Commands;
using Jewel.JPMS.Api.Features.UsefulInformation.Queries;
using Jewel.JPMS.Contracts.UsefulInformation;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.UsefulInformation;

public static class UsefulInformationFeatureRegistration
{
    public static IServiceCollection AddUsefulInformationFeature(this IServiceCollection services)
    {
        // The project's Useful Information tab: titled free-text notes for the office (door codes,
        // site notes). Internal roles read and manage alike — see UsefulInformationRoles.
        services.AddScoped<IQueryHandler<ListUsefulInformationForProject, IReadOnlyList<UsefulInformationNote>>, ListUsefulInformationForProjectHandler>();

        services.AddScoped<ICommandHandler<AddUsefulInformationNote, UsefulInformationNote>, AddUsefulInformationNoteHandler>();
        services.AddScoped<AddUsefulInformationNoteAuthorisation>();
        services.AddScoped<AddUsefulInformationNoteValidation>();

        services.AddScoped<ICommandHandler<UpdateUsefulInformationNote, UsefulInformationNote>, UpdateUsefulInformationNoteHandler>();
        services.AddScoped<UpdateUsefulInformationNoteAuthorisation>();
        services.AddScoped<UpdateUsefulInformationNoteValidation>();

        services.AddScoped<ICommandHandler<DeleteUsefulInformationNote, Acknowledgement>, DeleteUsefulInformationNoteHandler>();
        services.AddScoped<DeleteUsefulInformationNoteAuthorisation>();
        services.AddScoped<DeleteUsefulInformationNoteValidation>();

        return services;
    }
}
