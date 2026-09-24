namespace Jewel.JPMS.Contracts.Ai;

public enum SkillTextChange { Same, Removed, Added }

public sealed record SkillTextLine(SkillTextChange Change, string Text);

/// <summary>Two versions of a skill's text compared line by line — what was taken out, what was
/// put in, and what stayed. The longest run of lines the two share is kept; everything else is a
/// removal from the earlier text or an addition in the later one.</summary>
public static class SkillTextComparison
{
    /// <summary>Beyond this many line pairs in the changed middle the comparison stops looking for
    /// shared lines and reports the middle as replaced whole — a page must never hang on it.</summary>
    private const int MostLinePairs = 4_000_000;

    public static IReadOnlyList<SkillTextLine> Between(string earlier, string later)
    {
        var before = LinesOf(earlier);
        var after = LinesOf(later);
        var prefix = SharedLeadingLines(before, after);
        var suffix = SharedTrailingLines(before, after, prefix);

        var comparison = before.Take(prefix).Select(Same).ToList();
        comparison.AddRange(Middle(before[prefix..(before.Length - suffix)], after[prefix..(after.Length - suffix)]));
        comparison.AddRange(before.Skip(before.Length - suffix).Select(Same));
        return comparison;
    }

    private static string[] LinesOf(string text) => text.Replace("\r\n", "\n").Split('\n');

    private static SkillTextLine Same(string text) => new(SkillTextChange.Same, text);

    private static int SharedLeadingLines(string[] before, string[] after)
    {
        var count = 0;
        while (count < before.Length && count < after.Length && before[count] == after[count]) count++;
        return count;
    }

    private static int SharedTrailingLines(string[] before, string[] after, int prefix)
    {
        var count = 0;
        while (count < before.Length - prefix && count < after.Length - prefix
               && before[^(count + 1)] == after[^(count + 1)]) count++;
        return count;
    }

    private static IEnumerable<SkillTextLine> Middle(string[] before, string[] after)
    {
        var isTooLargeToAlign = (long)before.Length * after.Length > MostLinePairs;
        if (isTooLargeToAlign)
            return before.Select(line => new SkillTextLine(SkillTextChange.Removed, line))
                .Concat(after.Select(line => new SkillTextLine(SkillTextChange.Added, line)));
        return Aligned(before, after, SharedRunLengths(before, after));
    }

    private static int[,] SharedRunLengths(string[] before, string[] after)
    {
        var lengths = new int[before.Length + 1, after.Length + 1];
        for (var i = before.Length - 1; i >= 0; i--)
            for (var j = after.Length - 1; j >= 0; j--)
                lengths[i, j] = before[i] == after[j]
                    ? lengths[i + 1, j + 1] + 1
                    : Math.Max(lengths[i + 1, j], lengths[i, j + 1]);
        return lengths;
    }

    private static IEnumerable<SkillTextLine> Aligned(string[] before, string[] after, int[,] lengths)
    {
        var i = 0;
        var j = 0;
        while (i < before.Length || j < after.Length)
        {
            if (i < before.Length && j < after.Length && before[i] == after[j])
            {
                yield return Same(before[i++]);
                j++;
                continue;
            }
            var isRemovalNext = j == after.Length || (i < before.Length && lengths[i + 1, j] >= lengths[i, j + 1]);
            if (isRemovalNext) yield return new SkillTextLine(SkillTextChange.Removed, before[i++]);
            if (!isRemovalNext) yield return new SkillTextLine(SkillTextChange.Added, after[j++]);
        }
    }
}
