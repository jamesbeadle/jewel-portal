using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.DocumentControl;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.DocumentControl.Queries;

// The payment certificate register: all projects or one, newest issued first.
public sealed class ListPaymentCertificatesHandler
    : IQueryHandler<ListPaymentCertificates, IReadOnlyList<PaymentCertificate>>
{
    private readonly JpmsContext context;

    public ListPaymentCertificatesHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<PaymentCertificate>> HandleAsync(
        ListPaymentCertificates query, CancellationToken cancellationToken)
    {
        var rows = context.PaymentCertificates.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.ProjectId))
            rows = rows.Where(row => row.ProjectId == query.ProjectId);

        var certificates = await rows
            .OrderByDescending(row => row.IssuedDate)
            .ThenByDescending(row => row.CreatedAt)
            .ToListAsync(cancellationToken);
        return certificates.Select(row => row.ToModel()).ToList().AsReadOnly();
    }
}
