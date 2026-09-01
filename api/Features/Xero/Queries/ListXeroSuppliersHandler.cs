using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero.Queries;

/// <summary>
/// The Xero supplier list for the directory's "Import from Xero" modal. The Xero read is the
/// client's cached snapshot; each supplier is then stamped with whether a directory record already
/// links to it (and which), so the modal can show "Imported" instead of offering a second import.
/// </summary>
public sealed class ListXeroSuppliersHandler : IQueryHandler<ListXeroSuppliers, XeroSuppliersSnapshot>
{
    private readonly IXeroClient xero;
    private readonly JpmsContext context;

    public ListXeroSuppliersHandler(IXeroClient xero, JpmsContext context)
    {
        this.xero = xero;
        this.context = context;
    }

    public async Task<XeroSuppliersSnapshot> HandleAsync(ListXeroSuppliers query, CancellationToken cancellationToken)
    {
        var snapshot = await xero.GetSuppliersAsync(query.Force, cancellationToken);
        if (snapshot.Suppliers.Count == 0) return snapshot;

        var linkedByContactId = await context.SubcontractorXeroLinks.AsNoTracking()
            .ToDictionaryAsync(link => link.XeroContactId, link => link.SubcontractorId, cancellationToken);

        return snapshot with
        {
            Suppliers = snapshot.Suppliers
                .Select(supplier => linkedByContactId.TryGetValue(supplier.ContactId, out var subcontractorId)
                    ? supplier with { AlreadyImported = true, LinkedSubcontractorId = subcontractorId }
                    : supplier)
                .ToList()
        };
    }
}
