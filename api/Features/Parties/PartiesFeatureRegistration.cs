using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Parties;
using Jewel.JPMS.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Jewel.JPMS.Api.Features.Parties;

public static class PartiesFeatureRegistration
{
    public static IServiceCollection AddPartiesFeature(this IServiceCollection services)
    {
        // Party contacts — the people at a client account or architect practice, with each
        // person's default To/CC/BCC routing (the party's communication preferences).
        services.AddScoped<IQueryHandler<ListPartyContacts, IReadOnlyList<PartyContact>>, ListPartyContactsHandler>();
        services.AddScoped<ICommandHandler<UpsertPartyContact, PartyContact>, UpsertPartyContactHandler>();
        services.AddScoped<ICommandHandler<RemovePartyContact, Acknowledgement>, RemovePartyContactHandler>();
        services.AddScoped<PartyContactAuthorisation>();
        services.AddScoped<UpsertPartyContactValidation>();

        return services;
    }
}
