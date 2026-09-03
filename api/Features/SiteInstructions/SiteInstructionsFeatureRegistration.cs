using Jewel.JPMS.Api.Features.SiteInstructions.Commands;
using Jewel.JPMS.Api.Features.SiteInstructions.Queries;
using Jewel.JPMS.Contracts.SiteInstructions;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.SiteInstructions;

public static class SiteInstructionsFeatureRegistration
{
    public static IServiceCollection AddSiteInstructionsFeature(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<ListSiteInstructionsForProject, IReadOnlyList<SiteInstruction>>, ListSiteInstructionsForProjectHandler>();

        services.AddScoped<ICommandHandler<AddSiteInstruction, SiteInstruction>, AddSiteInstructionHandler>();
        services.AddScoped<AddSiteInstructionAuthorisation>();
        services.AddScoped<AddSiteInstructionValidation>();

        services.AddScoped<ICommandHandler<UpdateSiteInstruction, SiteInstruction>, UpdateSiteInstructionHandler>();
        services.AddScoped<UpdateSiteInstructionAuthorisation>();
        services.AddScoped<UpdateSiteInstructionValidation>();

        // The Control Centre's Internal-pathway "create new → Site instruction": raise + link
        // the originating email.
        services.AddScoped<ICommandHandler<CreateSiteInstructionFromMessage, SiteInstruction>, CreateSiteInstructionFromMessageHandler>();
        services.AddScoped<CreateSiteInstructionFromMessageAuthorisation>();
        services.AddScoped<CreateSiteInstructionFromMessageValidation>();

        return services;
    }
}
