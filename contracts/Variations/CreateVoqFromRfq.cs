using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Variations;

/// <summary>
/// Creates a Variation Order (in Quoting) from a request that has an RFQ enabled. One variation
/// order per request. Title/Description default to the request's when omitted. EstimatedValue
/// carries the reviewed figure from an AI draft (PrepareVoqDraft) when one was accepted.
/// </summary>
public sealed record CreateVoqFromRfq(
    string RequestId,
    string CreatedByEmail,
    string? Title = null,
    string? Description = null,
    decimal? EstimatedValue = null) : ICommand<VariationOrder>;
