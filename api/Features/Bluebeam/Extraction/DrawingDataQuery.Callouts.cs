using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>A note or specification callout and where it sits ([x, y] in mm). Text is clipped
/// for the reply; the row holds up to 2,000 characters and the blob the whole block.</summary>
public sealed record DrawingCalloutRow(string RevisionId, int Page, double[] At, string Text);

public static partial class DrawingDataQuery
{
    private const int CalloutReplyLength = 400;

    public static async Task<DrawingDataPage<DrawingCalloutRow>> CalloutsAsync(
        JpmsContext context, DrawingDataFilter filter, CancellationToken cancellationToken)
    {
        var matching = Callouts(context, filter);
        var count = await matching.CountAsync(cancellationToken);
        var rows = await Paged(matching
                .OrderBy(row => row.Page).ThenByDescending(row => row.Y).ThenBy(row => row.X), filter)
            .ToListAsync(cancellationToken);
        return new DrawingDataPage<DrawingCalloutRow>(new DrawingDataTotals(count), rows.Select(Row).ToList());
    }

    private static IQueryable<DrawingCalloutEntity> Callouts(JpmsContext context, DrawingDataFilter filter)
    {
        var rows = OnPage(InScope(context.DrawingCallouts.AsNoTracking(), filter), filter);
        var term = (filter.Contains ?? "").Trim();
        // SQL Server's default collation makes Contains case-insensitive, as a search should be.
        if (term.Length > 0) rows = rows.Where(row => row.Text.Contains(term));
        if (filter.Near is { } near)
            rows = rows.Where(row =>
                (row.X - near.X) * (row.X - near.X) + (row.Y - near.Y) * (row.Y - near.Y)
                <= near.WithinMm * near.WithinMm);
        return rows;
    }

    private static DrawingCalloutRow Row(DrawingCalloutEntity row) => new(
        row.DrawingRevisionId, row.Page, Point(row.X, row.Y),
        row.Text.Length <= CalloutReplyLength ? row.Text : row.Text[..CalloutReplyLength]);
}
