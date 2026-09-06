namespace Jewel.JPMS.Api.Features.Sales.Research;

public static class SalesQueues
{
    /// <summary>Strategies waiting for AI research (worker-consumed — several web searches and
    /// two long completions, minutes past the SWA gateway's patience).</summary>
    public const string StrategyResearch = "sales-strategy-research";
}

/// <summary>The queue message: which strategy to research. Who asked is on the row.</summary>
public sealed record StrategyResearchMessage(string StrategyId);

/// <summary>The imagine render queue (2026-09-06): one message per round the prospect submits —
/// Claude reads the photos, Azure image generation renders the concepts, the prospect is emailed.
/// Minutes of work, worker-consumed. Name lives here so the api producer and the worker consumer
/// can never drift.</summary>
public static class ImagineQueues
{
    public const string Render = "sales-imagine-render";
}

/// <summary>The render queue message: which round to render. Everything else is on the row.</summary>
public sealed record ImagineRenderMessage(string RoundId);
