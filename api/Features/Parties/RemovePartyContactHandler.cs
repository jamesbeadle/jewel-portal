using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Parties;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Parties;

/// <summary>
/// Removes a person from a party's contact book, along with any per-project routing overrides
/// that pointed at them (a dangling override row would otherwise sit inert forever). The party's
/// legacy contact-email field is left untouched so resolution keeps a fallback correspondent.
/// </summary>
public sealed class RemovePartyContactHandler : ICommandHandler<RemovePartyContact, Acknowledgement>
{
    private readonly JpmsContext context;
    public RemovePartyContactHandler(JpmsContext context) { this.context = context; }

    public async Task<Acknowledgement> HandleAsync(RemovePartyContact command, CancellationToken cancellationToken)
    {
        var entity = await context.PartyContacts.FirstOrDefaultAsync(
            c => c.PartyContactId == command.PartyContactId
                && c.PartyKind == (int)command.PartyKind && c.PartyId == command.PartyId,
            cancellationToken);
        if (entity is not null)
        {
            var overrides = await context.ProjectContacts
                .Where(c => c.PartyContactId == command.PartyContactId)
                .ToListAsync(cancellationToken);
            context.ProjectContacts.RemoveRange(overrides);
            context.PartyContacts.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
        }
        return new Acknowledgement(command.PartyContactId);
    }
}
