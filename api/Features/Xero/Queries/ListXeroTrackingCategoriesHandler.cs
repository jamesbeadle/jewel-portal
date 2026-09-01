using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Queries;

/// <summary>
/// The organisation's Xero tracking categories for the Cost codes page's "Xero sites" /
/// "Xero cost codes" tabs. A straight pass-through to the client's cached snapshot read —
/// the matching against system cost codes and project site mappings happens client-side,
/// where both masters are already loaded.
/// </summary>
public sealed class ListXeroTrackingCategoriesHandler
    : IQueryHandler<ListXeroTrackingCategories, XeroTrackingCategoriesSnapshot>
{
    private readonly IXeroClient xero;

    public ListXeroTrackingCategoriesHandler(IXeroClient xero) { this.xero = xero; }

    public Task<XeroTrackingCategoriesSnapshot> HandleAsync(
        ListXeroTrackingCategories query, CancellationToken cancellationToken) =>
        xero.GetTrackingCategoriesSnapshotAsync(query.Force, cancellationToken);
}
