using Jewel.JPMS.Api.Features.UsefulInformation.Commands;
using Jewel.JPMS.Api.Features.UsefulInformation.Queries;
using Jewel.JPMS.Contracts.UsefulInformation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.UsefulInformation;

public static class UsefulInformationFeatureRegistration
{
    public static IServiceCollection AddUsefulInformationFeature(this IServiceCollection services, IConfiguration configuration)
    {
        // The project's Useful Information tab: titled free-text notes for the office (door codes,
        // site notes). Internal roles read and manage alike — see UsefulInformationRoles.
        services.AddScoped<IQueryHandler<ListUsefulInformationForProject, IReadOnlyList<UsefulInformationNote>>, ListUsefulInformationForProjectHandler>();

        // The shared site credential a note may hold (2026-09-22): encrypted under the configured
        // key, set by anyone who manages notes, revealed by the directors alone and audited.
        services.AddSingleton(UsefulInformationOptions.FromConfiguration(configuration));
        services.AddSingleton<SecretProtector>();
        services.AddScoped<ICommandHandler<SetUsefulInformationSecret, UsefulInformationNote>, SetUsefulInformationSecretHandler>();
        services.AddScoped<SetUsefulInformationSecretAuthorisation>();
        services.AddScoped<SetUsefulInformationSecretValidation>();
        services.AddScoped<IQueryHandler<RevealUsefulInformationSecret, UsefulInformationSecret>, RevealUsefulInformationSecretHandler>();

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
