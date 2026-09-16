using System.ComponentModel.DataAnnotations;

namespace Jewel.JPMS.Api.Data.Entities;

/// <summary>
/// The transcription of a drawing revision as ROWS (2026-09-16) — one row per figured dimension,
/// callout and closed shape the extraction read from the PDF's own geometry. The structured read
/// still lives whole in the revision's structure blob (DrawingExtractions.StructureBlobRef); these
/// tables exist so work can be done OVER a drawing without loading that blob — filtered, paged
/// and totalled in SQL, across every drawing on a project — which is what the connector's
/// query_document_data and the estimating take-off need. Replaced wholesale on every successful
/// extraction (exactly as the Revu markup rows are); DrawingId/ProjectId are denormalised so a
/// project-wide read never joins through the revision. Every position is in real-world
/// millimetres from the sheet's bottom-left corner at the page's proven scale.
/// </summary>
public sealed class DrawingDimensionEntity
{
    [Key, MaxLength(64)] public string DrawingDimensionId { get; set; } = "";
    [MaxLength(64)]      public string DrawingExtractionId { get; set; } = "";
    [MaxLength(64)]      public string DrawingRevisionId { get; set; } = "";
    [MaxLength(64)]      public string DrawingId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int Page { get; set; }
    // The figure as written — the number a take-off uses (never the drawn length).
    public int ValueMm { get; set; }
    // H, V or D.
    [MaxLength(1)]       public string Axis { get; set; } = "";
    public double FromX { get; set; }
    public double FromY { get; set; }
    public double ToX { get; set; }
    public double ToY { get; set; }
    // Where the figure sits — the anchor for "which callout is this dimension nearest".
    public double LabelX { get; set; }
    public double LabelY { get; set; }
}

/// <summary>A note or specification callout on the sheet, with where it sits. Text is clipped to
/// the column — the blob keeps the full block.</summary>
public sealed class DrawingCalloutEntity
{
    public const int TextLength = 2000;

    [Key, MaxLength(64)] public string DrawingCalloutId { get; set; } = "";
    [MaxLength(64)]      public string DrawingExtractionId { get; set; } = "";
    [MaxLength(64)]      public string DrawingRevisionId { get; set; } = "";
    [MaxLength(64)]      public string DrawingId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int Page { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    [MaxLength(TextLength)] public string Text { get; set; } = "";
}

/// <summary>A closed vector shape — a pad, a slab outline, a chamber — with its size in real
/// units. Width/Height are the bounding box; perimeter and area are the polygon's own (approximate
/// when HasCurves — a circle reads as its inscribed polygon).</summary>
public sealed class DrawingShapeEntity
{
    [Key, MaxLength(64)] public string DrawingShapeId { get; set; } = "";
    [MaxLength(64)]      public string DrawingExtractionId { get; set; } = "";
    [MaxLength(64)]      public string DrawingRevisionId { get; set; } = "";
    [MaxLength(64)]      public string DrawingId { get; set; } = "";
    [MaxLength(64)]      public string ProjectId { get; set; } = "";
    public int Page { get; set; }
    public int PointCount { get; set; }
    public bool IsRectangle { get; set; }
    public bool HasCurves { get; set; }
    public double CentreX { get; set; }
    public double CentreY { get; set; }
    public double WidthMm { get; set; }
    public double HeightMm { get; set; }
    public double PerimeterMm { get; set; }
    public double AreaSqM { get; set; }
}
