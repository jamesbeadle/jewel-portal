using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Bluebeam.Extraction;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

// 2026-09-16: a drawing's structured read is transcribed into rows on every extraction, the rows
// are replaced wholesale on a re-extract, and the connector's query reads them filtered in SQL.
public sealed class DrawingDataRowsTests
{
    [Fact]
    public async Task Replace_writesOneRowPerItem_andReplacesThemOnTheNextRun()
    {
        await using var context = NewContext();
        var extraction = Extraction("rev-1", "dwg-1", "proj-1");
        await DrawingDataRows.ReplaceAsync(context, extraction, Structure(dimensions: 2, callouts: 3, shapes: 1), CancellationToken.None);
        await context.SaveChangesAsync();

        Assert.Equal(2, await context.DrawingDimensions.CountAsync());
        Assert.Equal(3, await context.DrawingCallouts.CountAsync());
        Assert.Equal(1, await context.DrawingShapes.CountAsync());
        Assert.NotNull(extraction.RowsWrittenAt);
        Assert.All(await context.DrawingCallouts.ToListAsync(), row => Assert.Equal("proj-1", row.ProjectId));

        await DrawingDataRows.ReplaceAsync(context, extraction, Structure(dimensions: 1, callouts: 0, shapes: 0), CancellationToken.None);
        await context.SaveChangesAsync();
        Assert.Equal(1, await context.DrawingDimensions.CountAsync());
        Assert.Equal(0, await context.DrawingCallouts.CountAsync());
        Assert.Equal(0, await context.DrawingShapes.CountAsync());
    }

    [Fact]
    public async Task Replace_clipsACalloutToTheColumn()
    {
        await using var context = NewContext();
        var longNote = new string('x', DrawingCalloutEntity.TextLength + 50);
        var structure = Structure(0, 0, 0) with { Callouts = new[] { new DrawingCallout(1, new DrawingPoint(1, 1), longNote) } };
        await DrawingDataRows.ReplaceAsync(context, Extraction("rev-1", "dwg-1", "proj-1"), structure, CancellationToken.None);
        await context.SaveChangesAsync();
        Assert.Equal(DrawingCalloutEntity.TextLength, (await context.DrawingCallouts.SingleAsync()).Text.Length);
    }

    [Fact]
    public async Task Query_filtersInTheDatabase_andTotalsEveryMatch()
    {
        await using var context = NewContext();
        var extraction = Extraction("rev-1", "dwg-1", "proj-1");
        var structure = new DrawingStructure(
            TitleBlock(), Array.Empty<DrawingRevisionNote>(), Array.Empty<DrawingPageScale>(),
            new[]
            {
                new DrawingDimension(1, 1200, "H", new DrawingPoint(0, 0), new DrawingPoint(1200, 0), new DrawingPoint(600, 50)),
                new DrawingDimension(1, 3000, "V", new DrawingPoint(0, 0), new DrawingPoint(0, 3000), new DrawingPoint(50, 1500)),
                new DrawingDimension(2, 450, "H", new DrawingPoint(0, 0), new DrawingPoint(450, 0), new DrawingPoint(225, 50))
            },
            new[]
            {
                new DrawingCallout(1, new DrawingPoint(600, 100), "C35 CONCRETE PAD"),
                new DrawingCallout(1, new DrawingPoint(9000, 9000), "Timber stud"),
                new DrawingCallout(2, new DrawingPoint(10, 10), "concrete blinding")
            },
            new[]
            {
                new DrawingShape(1, 4, true, false, new DrawingPoint(600, 300), 1200, 600, 3600, 0.72),
                new DrawingShape(1, 8, false, true, new DrawingPoint(8000, 8000), 500, 500, 1571, 0.196)
            },
            Array.Empty<string>());
        await DrawingDataRows.ReplaceAsync(context, extraction, structure, CancellationToken.None);
        await context.SaveChangesAsync();
        var scope = new[] { "rev-1" };

        var concrete = await DrawingDataQuery.CalloutsAsync(context, new DrawingDataFilter(scope, Contains: "concrete"), CancellationToken.None);
        Assert.Equal(2, concrete.Totals.Count);

        var nearThePad = await DrawingDataQuery.DimensionsAsync(context,
            new DrawingDataFilter(scope, Page: 1, Near: new DrawingDataNear(600, 100, 200)), CancellationToken.None);
        Assert.Single(nearThePad.Rows);
        Assert.Equal(1200, nearThePad.Rows[0].ValueMm);

        var horizontals = await DrawingDataQuery.DimensionsAsync(context, new DrawingDataFilter(scope, Axis: "h", Limit: 1), CancellationToken.None);
        Assert.Equal(2, horizontals.Totals.Count);
        Assert.Equal(1650d, horizontals.Totals.SumValueMm);
        Assert.Single(horizontals.Rows);

        var rectangles = await DrawingDataQuery.ShapesAsync(context, new DrawingDataFilter(scope, RectanglesOnly: true), CancellationToken.None);
        Assert.Single(rectangles.Rows);
        Assert.Equal(0.72, rectangles.Totals.SumAreaSqM);
        Assert.Equal(new[] { 600d, 300d }, rectangles.Rows[0].Centre);
    }

    [Fact]
    public async Task Scope_readsTheNewestExtractedRevisionOfEachDocument()
    {
        await using var context = NewContext();
        context.Drawings.Add(new DrawingEntity { DrawingId = "dwg-1", ProjectId = "proj-1", DrawingCode = "A-100", Title = "Plan" });
        context.Drawings.Add(new DrawingEntity { DrawingId = "dwg-2", ProjectId = "proj-1", DrawingCode = "A-200", Title = "Section" });
        context.DrawingRevisions.AddRange(
            Revision("rev-old", "dwg-1", "A", DateTimeOffset.UtcNow.AddDays(-2)),
            Revision("rev-new", "dwg-1", "B", DateTimeOffset.UtcNow.AddDays(-1)),
            Revision("rev-failed", "dwg-2", "A", DateTimeOffset.UtcNow));
        context.DrawingExtractions.AddRange(
            Extraction("rev-old", "dwg-1", "proj-1", rowsWrittenAt: DateTimeOffset.UtcNow),
            Extraction("rev-new", "dwg-1", "proj-1"),
            Extraction("rev-failed", "dwg-2", "proj-1", status: DrawingExtractionStatus.Failed));
        await context.SaveChangesAsync();

        var sheets = await DrawingDataScope.ResolveAsync(context, null, null, "proj-1", CancellationToken.None);
        var sheet = Assert.Single(sheets);
        Assert.Equal("rev-new", sheet.DrawingRevisionId);
        Assert.Equal("A-100", sheet.DrawingCode);
        Assert.Equal("B", sheet.RevisionLabel);
        Assert.False(sheet.HasRows);
    }

    private static JpmsContext NewContext() =>
        new(new DbContextOptionsBuilder<JpmsContext>().UseInMemoryDatabase($"drawing-data-{Guid.NewGuid():N}").Options);

    internal static DrawingExtractionEntity Extraction(
        string revisionId, string drawingId, string projectId,
        DrawingExtractionStatus status = DrawingExtractionStatus.Succeeded, DateTimeOffset? rowsWrittenAt = null) => new()
    {
        DrawingExtractionId = $"ext-{revisionId}", DrawingRevisionId = revisionId, DrawingId = drawingId, ProjectId = projectId,
        Status = (int)status, StructureBlobRef = $"blobs/{revisionId}/extraction-structure.json", RowsWrittenAt = rowsWrittenAt
    };

    internal static DrawingRevisionEntity Revision(string id, string drawingId, string label, DateTimeOffset receivedAt) => new()
    {
        DrawingRevisionId = id, DrawingId = drawingId, RevisionLabel = label, FileName = $"{id}.pdf",
        ContentType = "application/pdf", BlobRef = $"blobs/{id}.pdf", ReceivedAt = receivedAt
    };

    private static DrawingStructure Structure(int dimensions, int callouts, int shapes) => new(
        TitleBlock(), Array.Empty<DrawingRevisionNote>(), Array.Empty<DrawingPageScale>(),
        Enumerable.Range(0, dimensions).Select(i => new DrawingDimension(1, 100 * (i + 1), "H", new DrawingPoint(0, i), new DrawingPoint(100, i), new DrawingPoint(50, i))).ToList(),
        Enumerable.Range(0, callouts).Select(i => new DrawingCallout(1, new DrawingPoint(i, i), $"Note {i}")).ToList(),
        Enumerable.Range(0, shapes).Select(i => new DrawingShape(1, 4, true, false, new DrawingPoint(i, i), 10, 10, 40, 0.0001)).ToList(),
        Array.Empty<string>());

    private static DrawingTitleBlock TitleBlock() => new("A-100", "Plan", "B", "1:50", "A1", null, null, null, null, "");
}
