using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>A figured dimension: the value as written, its axis, the drawn line's ends and where
/// the figure sits — every position [x, y] in mm from the sheet's bottom-left corner.</summary>
public sealed record DrawingDimensionRow(
    string RevisionId, int Page, int ValueMm, string Axis, double[] From, double[] To, double[] At);

public static partial class DrawingDataQuery
{
    public static async Task<DrawingDataPage<DrawingDimensionRow>> DimensionsAsync(
        JpmsContext context, DrawingDataFilter filter, CancellationToken cancellationToken)
    {
        var matching = Dimensions(context, filter);
        var count = await matching.CountAsync(cancellationToken);
        var sum = count == 0 ? 0 : await matching.SumAsync(row => (long)row.ValueMm, cancellationToken);
        var rows = await Paged(matching
                .OrderBy(row => row.Page).ThenByDescending(row => row.LabelY).ThenBy(row => row.LabelX), filter)
            .ToListAsync(cancellationToken);
        return new DrawingDataPage<DrawingDimensionRow>(
            new DrawingDataTotals(count, SumValueMm: sum),
            rows.Select(Row).ToList());
    }

    private static IQueryable<DrawingDimensionEntity> Dimensions(JpmsContext context, DrawingDataFilter filter)
    {
        var rows = OnPage(InScope(context.DrawingDimensions.AsNoTracking(), filter), filter);
        var axis = (filter.Axis ?? "").Trim().ToUpperInvariant();
        if (axis.Length > 0) rows = rows.Where(row => row.Axis == axis);
        if (filter.MinValueMm is { } minimum) rows = rows.Where(row => row.ValueMm >= minimum);
        if (filter.MaxValueMm is { } maximum) rows = rows.Where(row => row.ValueMm <= maximum);
        if (filter.Near is { } near)
            rows = rows.Where(row =>
                (row.LabelX - near.X) * (row.LabelX - near.X) + (row.LabelY - near.Y) * (row.LabelY - near.Y)
                <= near.WithinMm * near.WithinMm);
        return rows;
    }

    private static DrawingDimensionRow Row(DrawingDimensionEntity row) => new(
        row.DrawingRevisionId, row.Page, row.ValueMm, row.Axis,
        Point(row.FromX, row.FromY), Point(row.ToX, row.ToY), Point(row.LabelX, row.LabelY));
}
