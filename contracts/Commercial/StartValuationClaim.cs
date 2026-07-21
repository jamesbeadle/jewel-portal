using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

public sealed record StartValuationClaim(
    string ProjectId,
    int ClaimNumber,
    DateTimeOffset ClaimDate,
    // Null (the normal path) = stamp from the project's retention terms at start time:
    // held % straight from the terms; release % applies only when the claim date has
    // reached practical completion. A value overrides — for seeding/backfill, not the UI.
    decimal? RetentionPercent = null,
    decimal? RetentionReleasePercent = null,
    // Free-text period name shown wherever the claim appears (e.g. "June 2026").
    // Optional; renameable at any status via RenameValuationClaim.
    string Name = "",
    // When set, the new claim opens with this claim's per-line % complete copied in
    // (cumulative rollover) instead of starting every line at 0%.
    string? SeedFromClaimId = null) : ICommand<ValuationClaim>;
