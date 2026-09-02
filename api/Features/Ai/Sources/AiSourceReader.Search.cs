using System.Text;

namespace Jewel.JPMS.Api.Features.Ai.Sources;

internal static partial class AiSourceReader
{
    /// query that matches nothing as a phrase falls back to "every word present") and parts whose
    /// NAME matches — the sheet called "V01 - Levelling compound" is the answer to "V01" before
    /// any row is.
    /// </summary>
    public static AiSourceSearchResult Search(AiSourceDocument document, string query, int maxHits = 20)
    {
        var wanted = Normalise(query);
        if (wanted.Length == 0 || document.IsImage)
            return new AiSourceSearchResult(Array.Empty<AiSourceHit>(), Array.Empty<AiSourceManifestPart>(), 0);

        var partsByName = document.Parts
            .Where(part => Normalise(part.Label).Contains(wanted, StringComparison.OrdinalIgnoreCase)
                           || Normalise(part.Key).Contains(wanted, StringComparison.OrdinalIgnoreCase))
            .Select(part => new AiSourceManifestPart(part.Key, part.Label, part.UnitName, part.Units.Count, part.Chars))
            .ToList();

        var hits = new List<AiSourceHit>();
        var total = 0;
        Collect(document, unitText => Normalise(unitText).Contains(wanted, StringComparison.OrdinalIgnoreCase), hits, ref total, maxHits, wanted);

        if (total == 0)
        {
            var words = wanted.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1)
            {
                Collect(document,
                    unitText =>
                    {
                        var normalised = Normalise(unitText);
                        return words.All(word => normalised.Contains(word, StringComparison.OrdinalIgnoreCase));
                    },
                    hits, ref total, maxHits, words[0]);
            }
        }

        return new AiSourceSearchResult(hits, partsByName, total);
    }

    private static void Collect(
        AiSourceDocument document, Func<string, bool> matches, List<AiSourceHit> hits, ref int total, int maxHits, string anchor)
    {
        foreach (var part in document.Parts)
        {
            for (var unit = 1; unit <= part.Units.Count; unit++)
            {
                var text = part.Units[unit - 1];
                if (text.Length == 0 || !matches(text)) continue;
                total++;
                if (hits.Count < maxHits)
                    hits.Add(new AiSourceHit(part.Key, part.Label, unit, Snippet(text, anchor)));
            }
        }
    }

    /// <summary>Up to ~240 characters around the first occurrence — enough to read the row, not
    /// the whole 40-column line.</summary>
    private static string Snippet(string text, string anchor)
    {
        const int width = 240;
        if (text.Length <= width) return text;
        var at = text.IndexOf(anchor, StringComparison.OrdinalIgnoreCase);
        var start = Math.Max(0, Math.Min(at < 0 ? 0 : at - 60, text.Length - width));
        var piece = text.Substring(start, Math.Min(width, text.Length - start));
        return (start > 0 ? "…" : "") + piece + (start + piece.Length < text.Length ? "…" : "");
    }

    private static string Normalise(string value)
    {
        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;
        foreach (var character in value.Trim())
        {
            if (char.IsWhiteSpace(character)) { pendingSpace = true; continue; }
            if (pendingSpace && builder.Length > 0) builder.Append(' ');
            pendingSpace = false;
            builder.Append(character);
        }
        return builder.ToString();
    }
}
