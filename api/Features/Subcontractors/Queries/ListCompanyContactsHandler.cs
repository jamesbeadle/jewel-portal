using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.Queries;

public sealed class ListCompanyContactsHandler
    : IQueryHandler<ListCompanyContacts, IReadOnlyList<CompanyContact>>
{
    private readonly JpmsContext context;

    public ListCompanyContactsHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<CompanyContact>> HandleAsync(ListCompanyContacts query, CancellationToken cancellationToken)
    {
        var entities = await context.CompanyContacts.AsNoTracking()
            .Where(contact => contact.SubcontractorId == query.SubcontractorId)
            .OrderBy(contact => contact.Name)
            .ToListAsync(cancellationToken);
        return entities.Select(entity => entity.ToModel()).ToList().AsReadOnly();
    }
}
