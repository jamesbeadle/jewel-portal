namespace Jewel.JPMS.Api.Features.Sales.Research;

public static class SalesQueues
{
    /// <summary>Strategies waiting for AI research (worker-consumed — several web searches and
    /// two long completions, minutes past the SWA gateway's patience).</summary>
    public const string StrategyResearch = "sales-strategy-research";
}

/// <summary>The queue message: which strategy to research. Who asked is on the row.</summary>
public sealed record StrategyResearchMessage(string StrategyId);
