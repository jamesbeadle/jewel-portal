using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

/// <summary>A circle on the sheet, in real-world millimetres — "within 2 m of this callout".</summary>
public sealed record DrawingDataNear(double X, double Y, double WithinMm);

/// <summary>
/// What a data query asks for. Every filter is optional and they combine; the kind-specific ones
/// (Axis and the value range for dimensions, Contains for callouts, the area range and
/// RectanglesOnly for shapes) are ignored by the other kinds. Offset/Limit page the result.
/// </summary>
public sealed record DrawingDataFilter(
    IReadOnlyList<string> RevisionIds,
    int? Page = null,
    string? Contains = null,
    string? Axis = null,
    int? MinValueMm = null,
    int? MaxValueMm = null,
    double? MinAreaSqM = null,
    double? MaxAreaSqM = null,
    bool RectanglesOnly = false,
    DrawingDataNear? Near = null,
    int Offset = 0,
    int Limit = 50);

/// <summary>Totals over EVERY row the filter matches, not just the page returned — so a take-off
/// can sum a whole sheet in one call.</summary>
public sealed record DrawingDataTotals(int Count, double? SumValueMm = null, double? SumAreaSqM = null, double? SumPerimeterMm = null);

public sealed record DrawingDataPage<TRow>(DrawingDataTotals Totals, IReadOnlyList<TRow> Rows);

/// <summary>
/// The queries over the transcription rows (2026-09-16), one per kind, each translated to SQL
/// whole — filter, count, sum, page — so the api never loads a sheet's rows to answer a question
/// about some of them. Positions come back rounded to the millimetre, areas to three places.
/// </summary>
public static partial class DrawingDataQuery
{
    private const int MillimetrePlaces = 0;
    private const int AreaPlaces = 3;

    private static double Mm(double value) => Math.Round(value, MillimetrePlaces);
    private static double Area(double value) => Math.Round(value, AreaPlaces);
    private static double[] Point(double x, double y) => new[] { Mm(x), Mm(y) };

    private static IQueryable<TRow> InScope<TRow>(IQueryable<TRow> rows, DrawingDataFilter filter)
        where TRow : class =>
        rows.Where(row => filter.RevisionIds.Contains(EF.Property<string>(row, nameof(DrawingCalloutEntity.DrawingRevisionId))));

    private static IQueryable<TRow> OnPage<TRow>(IQueryable<TRow> rows, DrawingDataFilter filter)
        where TRow : class =>
        filter.Page is { } page
            ? rows.Where(row => EF.Property<int>(row, nameof(DrawingCalloutEntity.Page)) == page)
            : rows;

    private static IQueryable<TRow> Paged<TRow>(IQueryable<TRow> ordered, DrawingDataFilter filter) =>
        ordered.Skip(Math.Max(0, filter.Offset)).Take(Math.Max(1, filter.Limit));
}
