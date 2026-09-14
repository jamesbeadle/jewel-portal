
namespace Jewel.JPMS.Api.Features.Variations.Documents;

/// <summary>
/// Everything needed to render a variation order's official document, collated from the SQL source
/// of truth. A flat, self-contained snapshot — the renderer has no database dependency, and the
/// bytes are a pure function of the current record (bar <see cref="GeneratedAt"/>), so regeneration
/// on download, attach and resend is idempotent. Same arrangement as RequestDocumentModel.
/// </summary>
public sealed record VariationDocumentModel(
    string VariationOrderId,
    // "VO31" — the reference the document goes out under (header, file name, PDF title). The
    // quoting reference ("VOQ-0031") is internal and deliberately NOT carried on this model, so no
    // renderer can print it on an outgoing document (Nigel, 2026-09-14).
    string DocumentReference,
    string Title,
    string Description,
    string StatusLabel,
    string ProjectName,
    string ProjectReference,
    string ClientName,
    string CreatedByEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset? IssuedAt,      // stamped when the order enters Issued; null while quoting
    DateTimeOffset? ApprovedAt,
    string? VariationRef,          // minted at approval; null until then
    decimal? EstimatedValue,       // the quoting-stage estimate
    decimal ApprovedValue,         // the agreed (contract) value; 0 until approved
    bool IsApproved,
    // ---- Narrative sections: all optional, rendered only when present -------------------------
    string? CommercialBasis,
    string? ProgrammeImpact,
    string? Exclusions,
    // The line build-up: the priced lines as they stand on the valuation report once approved,
    // otherwise the staged (agreed / quoted) lines held on the record — the document shows the
    // line detail at every stage. Empty only when nothing has been staged yet.
    IReadOnlyList<VariationDocumentLine> Lines,
    // True when Lines is the staged build-up rather than the report's lines — the sheet says so,
    // because staged lines have not been written anywhere commercial yet.
    bool LinesAreStaged,
    DateTimeOffset GeneratedAt)
{
    /// <summary>The date the document presents as its issue date: the recorded client-issue date
    /// once the order has been issued, otherwise the created date (a quoting-stage render has no
    /// better client-facing date than when the record was raised).</summary>
    public DateTimeOffset IssuedDisplayDate => IssuedAt ?? CreatedAt;

    /// <summary>The build-up's sum — the figure the cost breakdown totals to.</summary>
    public decimal LinesTotal => Lines.Sum(line => line.Amount);

    /// <summary>A safe, human file name for the PDF — "VO31 - Staircase Enclosure Ply.pdf",
    /// falling back to the document name before a number exists.</summary>
    public string FileName
    {
        get
        {
            var title = Title.Trim();
            if (title.Length > 60) title = title[..60].TrimEnd();
            var stem = DocumentReference.Length > 0 ? DocumentReference : DocumentName;
            if (title.Length > 0) stem = $"{stem} - {title}";
            foreach (var invalid in Path.GetInvalidFileNameChars())
                stem = stem.Replace(invalid, '-');
            return stem + ".pdf";
        }
    }

    /// <summary>The email subject line used when the document is sent or drafted.</summary>
    public string EmailSubject => $"{DocumentReference} {DocumentName}: {Title} — {ProjectName}".Trim();

    /// <summary>What the document calls itself — its heading, PDF title and file-name fallback.</summary>
    public const string DocumentName = "Variation Order";
}

/// <summary>One priced row of the cost breakdown (Cost code / Description / Qty / Unit / Rate / Amount).</summary>
public sealed record VariationDocumentLine(
    string CostCode,
    string Description,
    string Unit,
    decimal Quantity,
    decimal Rate,
    decimal Amount);
