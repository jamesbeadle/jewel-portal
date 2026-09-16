using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>A closed shape: its centre ([x, y] in mm), bounding box, perimeter and area (m²);
/// HasCurves means the perimeter and area are approximate.</summary>
public sealed record DrawingShapeRow(
    string RevisionId, int Page, double[] Centre, double WidthMm, double HeightMm,
    double PerimeterMm, double AreaSqM, bool IsRectangle, bool HasCurves);

public static partial class DrawingDataQuery
{
    public static async Task<DrawingDataPage<DrawingShapeRow>> ShapesAsync(
        JpmsContext context, DrawingDataFilter filter, CancellationToken cancellationToken)
    {
        var matching = Shapes(context, filter);
        var count = await matching.CountAsync(cancellationToken);
        var area = count == 0 ? 0 : await matching.SumAsync(row => row.AreaSqM, cancellationToken);
        var perimeter = count == 0 ? 0 : await matching.SumAsync(row => row.PerimeterMm, cancellationToken);
        var rows = await Paged(matching.OrderByDescending(row => row.AreaSqM).ThenBy(row => row.DrawingShapeId), filter)
            .ToListAsync(cancellationToken);
        return new DrawingDataPage<DrawingShapeRow>(
            new DrawingDataTotals(count, SumAreaSqM: Area(area), SumPerimeterMm: Mm(perimeter)),
            rows.Select(Row).ToList());
    }

    private static IQueryable<DrawingShapeEntity> Shapes(JpmsContext context, DrawingDataFilter filter)
    {
        var rows = OnPage(InScope(context.DrawingShapes.AsNoTracking(), filter), filter);
        if (filter.MinAreaSqM is { } minimum) rows = rows.Where(row => row.AreaSqM >= minimum);
        if (filter.MaxAreaSqM is { } maximum) rows = rows.Where(row => row.AreaSqM <= maximum);
        if (filter.RectanglesOnly) rows = rows.Where(row => row.IsRectangle);
        if (filter.Near is { } near)
            rows = rows.Where(row =>
                (row.CentreX - near.X) * (row.CentreX - near.X) + (row.CentreY - near.Y) * (row.CentreY - near.Y)
                <= near.WithinMm * near.WithinMm);
        return rows;
    }

    private static DrawingShapeRow Row(DrawingShapeEntity row) => new(
        row.DrawingRevisionId, row.Page, Point(row.CentreX, row.CentreY),
        Mm(row.WidthMm), Mm(row.HeightMm), Mm(row.PerimeterMm), Area(row.AreaSqM),
        row.IsRectangle, row.HasCurves);
}
