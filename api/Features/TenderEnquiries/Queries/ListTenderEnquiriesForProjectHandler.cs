using Jewel.JPMS.Api.Cqrs;
using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Contracts.TenderEnquiries;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.TenderEnquiries.Queries;

/// <summary>A project's enquiries, newest received first — ReceivedAt is the official date lists
/// lead with; CreatedAt is the system stamp.</summary>
public sealed class ListTenderEnquiriesForProjectHandler
    : IQueryHandler<ListTenderEnquiriesForProject, IReadOnlyList<TenderEnquiry>>
{
    private readonly JpmsContext context;

    public ListTenderEnquiriesForProjectHandler(JpmsContext context) { this.context = context; }

    public async Task<IReadOnlyList<TenderEnquiry>> HandleAsync(
        ListTenderEnquiriesForProject query, CancellationToken cancellationToken)
    {
        var rows = await context.TenderEnquiries.AsNoTracking()
            .Where(row => row.ProjectId == query.ProjectId)
            .OrderByDescending(row => row.ReceivedAt)
            .ThenByDescending(row => row.Number)
            .ToListAsync(cancellationToken);
        return rows.Select(row => row.ToModel()).ToList();
    }
}

public sealed class GetTenderEnquiryByIdHandler : IQueryHandler<GetTenderEnquiryById, TenderEnquiry?>
{
    private readonly JpmsContext context;

    public GetTenderEnquiryByIdHandler(JpmsContext context) { this.context = context; }

    public async Task<TenderEnquiry?> HandleAsync(GetTenderEnquiryById query, CancellationToken cancellationToken)
    {
        var row = await context.TenderEnquiries.AsNoTracking()
            .FirstOrDefaultAsync(entity => entity.TenderEnquiryId == query.TenderEnquiryId, cancellationToken);
        return row?.ToModel();
    }
}

public sealed class ListTenderEnquiryAnswersHandler
    : IQueryHandler<ListTenderEnquiryAnswers, IReadOnlyList<TenderEnquiryAnswer>>
{
    private readonly JpmsContext context;

    public ListTenderEnquiryAnswersHandler(JpmsContext context) { this.context = context; }

    public Task<IReadOnlyList<TenderEnquiryAnswer>> HandleAsync(
        ListTenderEnquiryAnswers query, CancellationToken cancellationToken) =>
        TenderEnquiryAnswerReader.ListAsync(context, query.TenderEnquiryId, cancellationToken);
}
