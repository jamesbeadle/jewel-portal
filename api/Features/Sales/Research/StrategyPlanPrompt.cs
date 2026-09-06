namespace Jewel.JPMS.Api.Features.Sales.Commands;

// The approach-plan prompt — shared with the worker (linked source), which drafts the plan at the
// end of a research run; the API's GenerateStrategyApproachPlanHandler uses the same text.
internal static class StrategyPlanPrompt
{
    public const string System =
        "You are the sales strategist for Jewel Bespoke Build, a high-end residential builder in the UK: "
        + "bespoke new homes and substantial upgrades (extensions, refurbishments, whole-house remodels) for "
        + "private clients, usually working with an architect. You write approach plans for finding leads. "
        + "A plan is a working document the team will follow, not marketing copy. Write in British English, "
        + "plainly, in markdown with short headed sections. Be concrete: name the exact people to approach "
        + "and how to find them, what to say and why it is credible to THEM, the steps in order with rough "
        + "timing, what to measure at each step, and the evidence that would show the hypothesis is wrong "
        + "so the strategy can be stopped early. Where the strategy rests on data (house prices, planning "
        + "applications, infrastructure, company records) say which public sources to pull and what to look "
        + "for in them — do not invent figures. Where the audience is architects, sell what Jewel's project "
        + "portal does for their job (drawings tracked to revision, RFIs and variations with one number "
        + "through every stage, a programme everyone can see, correspondence filed to the record) and the "
        + "errors and chasing it removes. Keep it under 900 words. No preamble, no sign-off — start with the "
        + "first heading.";

    public static string User(SalesStrategy strategy, string? guidance)
    {
        var lines = new List<string>
        {
            $"Strategy: {strategy.Name}",
            "",
            "The brief — the idea in the team's own words:",
            string.IsNullOrWhiteSpace(strategy.Brief) ? "(none written)" : strategy.Brief,
            "",
            $"Audience: {strategy.Audience.DisplayName()}",
            $"Target area: {(string.IsNullOrWhiteSpace(strategy.TargetArea) ? "(not given)" : strategy.TargetArea)}",
            $"Channel: {strategy.Channel.DisplayName()}",
            "",
            "Hypothesis — why these people, why now:",
            string.IsNullOrWhiteSpace(strategy.Hypothesis) ? "(not written yet — propose one and say it is a proposal)" : strategy.Hypothesis,
            "",
            "Evidence and data sources so far:",
            string.IsNullOrWhiteSpace(strategy.Evidence) ? "(none recorded — say what should be gathered first)" : strategy.Evidence,
            "",
            "Proposition — what we would say to them:",
            string.IsNullOrWhiteSpace(strategy.Proposition) ? "(not written yet — draft one)" : strategy.Proposition
        };
        if (!string.IsNullOrWhiteSpace(strategy.ResearchFindings))
        {
            lines.Add("");
            lines.Add("Research findings (with sources) — build the plan on these, and cite the source where a step rests on a finding:");
            lines.Add(strategy.ResearchFindings.Length > 9000 ? strategy.ResearchFindings[..9000] : strategy.ResearchFindings);
        }
        if (!string.IsNullOrWhiteSpace(guidance))
        {
            lines.Add("");
            lines.Add("Steer from the team:");
            lines.Add(guidance.Trim());
        }
        if (!string.IsNullOrWhiteSpace(strategy.ApproachPlan))
        {
            lines.Add("");
            lines.Add("There is an existing plan; write a fresh one rather than editing it, keeping anything in it that is clearly a decision already made:");
            lines.Add(strategy.ApproachPlan.Length > 6000 ? strategy.ApproachPlan[..6000] : strategy.ApproachPlan);
        }
        lines.Add("");
        lines.Add("Write the approach plan.");
        return string.Join("\n", lines);
    }
}
