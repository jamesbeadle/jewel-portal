using System.Text;
using Jewel.JPMS.Contracts.Progress;

namespace Jewel.JPMS.Api.Features.Progress.ContractorsReports.Composition;

/// <summary>
/// The one-shot prompt that turns the week's daily logs into Section 1 in house language, and
/// the parser for what comes back. The rules are Jeremy's of 21 Sep 2026: strip progress hedges
/// and move the unfinished half to Look Ahead, never upgrade a material or method word into a
/// compliance claim, never drop a completed item silently, flag a conflict rather than pick a
/// side, write the narrow scope and flag it. Strict JSON, as every one-shot prompt in the API;
/// a day outside the week is dropped by the parser, never trusted.
/// </summary>
internal static class ContractorsReportRewritePrompt
{
    private const string DateFormat = "yyyy-MM-dd";

    public static string System =>
        "You are the office of a UK residential main contractor writing Section 1 (Progress Against "
        + "Programme) of the weekly Contractor's Report from the site team's own daily logs. The report "
        + "goes to the client's contract administrator, so it states what was done, plainly and "
        + "accurately, and admits nothing.\n\n"
        + "Rules:\n"
        + "- Rewrite each day's notes as short bullets in plain past-tense house language: what was done "
        + "and where. No names, no hours, no cost codes, no rates; drop lines such as \"Cost code:\", "
        + "\"Hours:\", \"On site:\" — they are for the office, not the report.\n"
        + $"- Never write any of these words: {string.Join(", ", ReportWordingRule.BannedPhrases)}. Work that puts "
        + "something right is not reported: leave it out and flag it as compliance, saying what was left out.\n"
        + "- Keep the site's own material and method words. Never upgrade one into a compliance claim: "
        + "\"foamed\" stays \"foamed\", never \"fire stopped\". Where a word could be read as a compliance "
        + "claim, keep the site's word and flag it as compliance.\n"
        + "- Strip progress hedges (\"still more to be done\", \"further works to follow\", \"ongoing\") "
        + "from an item being claimed; put the unfinished half in lookAhead and flag it as hedge.\n"
        + "- Never drop a completed item silently. Anything left out goes in the flags as omitted, with why.\n"
        + "- Where two people describe the same work differently — one complete, one in progress — write "
        + "the narrower version and flag it as conflict. Do not resolve it.\n"
        + "- Match scope precisely: \"rad fitted\" is one radiator, not radiators completed. Where quantity "
        + "or completion is ambiguous, write the narrow version and flag it as scope.\n"
        + "- Every change of meaning beyond tidying the grammar gets a changed flag saying what and why.\n"
        + "- One sentence of summary per day.\n\n"
        + "Answer with STRICT JSON only — no markdown fences, no commentary:\n"
        + "{\"days\":[{\"date\":\"YYYY-MM-DD\",\"summary\":\"…\",\"bullets\":[\"…\"]}],"
        + "\"lookAhead\":[\"…\"],"
        + "\"flags\":[{\"kind\":\"changed|compliance|hedge|omitted|conflict|scope\",\"date\":\"YYYY-MM-DD or null\",\"text\":\"…\"}]}";

    public static string User(ContractorsReportHeader header, IReadOnlyList<ContractorsReportDay> days)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"PROJECT: {header.ProjectName} ({header.ProjectReference})");
        builder.AppendLine($"PERIOD: {header.PeriodStart.ToString(DateFormat)} to {header.PeriodEnd.ToString(DateFormat)}");
        builder.AppendLine();
        builder.AppendLine("THE DAILY LOGS, DAY BY DAY (each entry is one person's own words, kept exactly as typed):");
        foreach (var day in days)
        {
            builder.AppendLine($"DAY {day.Date.ToString(DateFormat)} — {day.Heading}");
            foreach (var entry in day.Entries)
                builder.AppendLine($"- [{entry.Title}] {entry.Description.ReplaceLineEndings(" / ")}");
        }
        builder.AppendLine();
        builder.AppendLine("Rewrite every day listed. STRICT JSON only.");
        return builder.ToString();
    }

    /// <summary>Null when nothing usable came back. Tolerates fences and prose around the JSON;
    /// keeps only days inside the week; an unknown flag kind reads as a change.</summary>
    public static ContractorsReportRewrite? Parse(string? responseText, ReportingWeek week, DateTimeOffset requestedAt)
    {
        if (string.IsNullOrWhiteSpace(responseText)) return null;
        var start = responseText.IndexOf('{');
        var end = responseText.LastIndexOf('}');
        if (start < 0 || end <= start) return null;
        try
        {
            using var document = JsonDocument.Parse(responseText[start..(end + 1)]);
            var root = document.RootElement;
            var days = Array(root, "days").Select(day => DayOf(day, week)).Where(day => day is not null).Select(day => day!).ToList();
            if (days.Count == 0) return null;
            var lookAhead = Array(root, "lookAhead").Select(Text).Where(line => line.Length > 0).ToList();
            var flags = Array(root, "flags").Select(flag => FlagOf(flag, week)).ToList();
            return new ContractorsReportRewrite(requestedAt, days, lookAhead, flags);
        }
        catch (JsonException) { return null; }
    }

    private static IEnumerable<JsonElement> Array(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.Array
            ? element.EnumerateArray()
            : Enumerable.Empty<JsonElement>();

    private static string Text(JsonElement element) =>
        element.ValueKind == JsonValueKind.String ? (element.GetString() ?? "").Trim() : "";

    private static string Text(JsonElement parent, string name) =>
        parent.TryGetProperty(name, out var element) ? Text(element) : "";

    private static DateOnly? DateIn(JsonElement parent, ReportingWeek week)
    {
        var isDate = DateOnly.TryParseExact(Text(parent, "date"), DateFormat, out var date);
        return isDate && week.Contains(date) ? date : null;
    }

    private static ContractorsReportRewrittenDay? DayOf(JsonElement element, ReportingWeek week)
    {
        var date = DateIn(element, week);
        if (date is null) return null;
        var bullets = Array(element, "bullets").Select(Text).Where(bullet => bullet.Length > 0).ToList();
        return new ContractorsReportRewrittenDay(date.Value, Text(element, "summary"), bullets);
    }

    private static ContractorsReportFlag FlagOf(JsonElement element, ReportingWeek week) =>
        new(KindOf(Text(element, "kind")), DateIn(element, week), Text(element, "text"));

    private static ContractorsReportFlagKind KindOf(string kind) =>
        Enum.TryParse<ContractorsReportFlagKind>(kind, ignoreCase: true, out var known) ? known : ContractorsReportFlagKind.Changed;
}
