using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Features.TenderEnquiries;

/// <summary>The questionnaire in question order — one read shared by the query, the save and the
/// document builder so every route sees the same sheet.</summary>
internal static class TenderEnquiryAnswerReader
{
    public static async Task<IReadOnlyList<TenderEnquiryAnswer>> ListAsync(
        JpmsContext context, string tenderEnquiryId, CancellationToken cancellationToken)
    {
        var rows = await context.TenderEnquiryAnswers.AsNoTracking()
            .Where(row => row.TenderEnquiryId == tenderEnquiryId)
            .OrderBy(row => row.Position)
            .ToListAsync(cancellationToken);
        return rows.Select(row => row.ToModel()).ToList();
    }
}
