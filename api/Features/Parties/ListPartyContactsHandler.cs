using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.Parties;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.Parties;

public sealed class ListPartyContactsHandler : IQueryHandler<ListPartyContacts, IReadOnlyList<PartyContact>>
{
    private readonly JpmsContext context;
    public ListPartyContactsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<PartyContact>> HandleAsync(ListPartyContacts query, CancellationToken cancellationToken)
    {
        var entities = await context.PartyContacts
            .Where(c => c.PartyKind == (int)query.PartyKind && c.PartyId == query.PartyId)
            .OrderByDescending(c => c.IsPrimary)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
