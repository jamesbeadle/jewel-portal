using System.Text;

namespace Jewel.JPMS.Api.Features.Ai.Sources;

internal static partial class AiSourceReader
{

    /// <summary>
    /// Reads from a position under a character budget. With <paramref name="partKey"/> given the
    /// read stays inside that part (the model asked for the V01 tab, not the V01 tab and half of
    /// V02) and <c>Next</c> points at the following part when there is one; with it omitted the
    /// read starts at the first part and flows across part boundaries — each announced by a
    /// header line — until the budget is spent, so a twelve-page PDF reads in a handful of calls.
    /// At least one unit is always returned, whatever the budget.
    /// </summary>
    public static AiSourceReadResult Read(AiSourceDocument document, string? partKey, int from, int maxChars)
    {
        if (document.Parts.Count == 0)
            return new AiSourceReadResult("", "", "", 0, 0, true, null);

        var explicitPart = !string.IsNullOrWhiteSpace(partKey);
        var startIndex = 0;
        if (explicitPart)
        {
            var part = document.Part(partKey)
                ?? throw new ArgumentException($"No part named \"{partKey}\".", nameof(partKey));
            startIndex = document.Parts.ToList().IndexOf(part);
        }

        maxChars = Math.Clamp(maxChars, MinReadChars, MaxReadChars);
        var text = new StringBuilder();
        var startPart = document.Parts[startIndex];
        var fromUnit = Math.Max(1, from);
        var toUnit = fromUnit - 1;
        AiSourcePosition? next = null;
        var reachedEnd = false;
        var anyUnit = false;

        for (var partIndex = startIndex; partIndex < document.Parts.Count; partIndex++)
        {
            var part = document.Parts[partIndex];
            var first = partIndex == startIndex ? fromUnit : 1;

            if (first > part.Units.Count && partIndex == startIndex && part.Units.Count > 0)
            {
                // Asked to start past the end of the part: nothing here, say so honestly.
                return new AiSourceReadResult(
                    $"[{Header(document, part)} has {part.Units.Count} {part.UnitName}s — nothing from {first} onwards.]",
                    part.Key, part.Label, first, part.Units.Count, true,
                    partIndex + 1 < document.Parts.Count ? new AiSourcePosition(document.Parts[partIndex + 1].Key, 1) : null);
            }

            if (text.Length > 0) text.AppendLine();
            text.AppendLine($"[{Header(document, part)}{(first > 1 ? $" — from {part.UnitName} {first}" : "")}]");

            var stoppedInside = false;
            for (var unit = first; unit <= part.Units.Count; unit++)
            {
                var line = FormatUnit(document, part, unit);
                if (anyUnit && text.Length + line.Length + 1 > maxChars)
                {
                    next = new AiSourcePosition(part.Key, unit);
                    stoppedInside = true;
                    break;
                }
                text.AppendLine(line);
                anyUnit = true;
                toUnit = unit;
            }

            if (stoppedInside)
            {
                text.AppendLine($"[… continues at {part.UnitName} {next!.From} of {Header(document, part)} — call read_source again with part \"{part.Key}\" and from {next.From}.]");
                return new AiSourceReadResult(text.ToString().TrimEnd(), startPart.Key, startPart.Label, fromUnit, toUnit, false, next);
            }

            // The part is finished. Stop here when the caller named it; otherwise flow on.
            var hasMoreParts = partIndex + 1 < document.Parts.Count;
            if (explicitPart || !hasMoreParts)
            {
                reachedEnd = true;
                next = hasMoreParts ? new AiSourcePosition(document.Parts[partIndex + 1].Key, 1) : null;
                if (hasMoreParts)
                    text.AppendLine($"[End of {Header(document, part)}. Next part: «{document.Parts[partIndex + 1].Label}».]");
                return new AiSourceReadResult(text.ToString().TrimEnd(), startPart.Key, startPart.Label, fromUnit, toUnit, reachedEnd, next);
            }
        }

        return new AiSourceReadResult(text.ToString().TrimEnd(), startPart.Key, startPart.Label, fromUnit, toUnit, true, null);
    }

    /// <summary>The opening of the first part, for the Context row: what the file is at a glance.</summary>
    public static string Preview(AiSourceDocument document, int maxChars = PreviewChars)
    {
        if (document.IsImage || document.Parts.Count == 0) return "";
        var first = document.Parts[0];
        var text = new StringBuilder();
        text.AppendLine($"[{Header(document, first)} — opening {first.UnitName}s]");
        for (var unit = 1; unit <= first.Units.Count; unit++)
        {
            var line = FormatUnit(document, first, unit);
            if (text.Length + line.Length + 1 > maxChars) break;
            text.AppendLine(line);
        }
        return text.ToString().TrimEnd();
    }

    private static string Header(AiSourceDocument document, AiSourcePart part) => document.Kind switch
    {
        AiSourceDocument.Workbook => $"Sheet: {part.Label}",
        AiSourceDocument.Pdf => part.Label,
        _ => part.Label
    };

    /// <summary>Rows and lines carry their number so "row 12" can be quoted and paged to; a
    /// PDF's lines and a document's paragraphs read better bare (the page IS the unit people
    /// cite, and search hits still give the number).</summary>
    private static string FormatUnit(AiSourceDocument document, AiSourcePart part, int unit) =>
        document.Kind is AiSourceDocument.Workbook or AiSourceDocument.Text
            ? $"{unit}\t{part.Units[unit - 1]}"
            : part.Units[unit - 1];

    // ---- Search -----------------------------------------------------------------------------
}
