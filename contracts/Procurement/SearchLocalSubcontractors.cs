using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Procurement;

// Finds companies of a given trade near a project's site via Google Places text search, so bid
// packages can be tendered beyond the existing directory. Location comes from the project's
// town/postcode (set in Edit details). Trade is free-ish text — the UI offers the directory's
// existing trades. Pass the previous result's NextPageToken to fetch the next page.
public sealed record SearchLocalSubcontractors(
    string ProjectId,
    string Trade,
    string? PageToken = null) : IQuery<LocalSubcontractorSearchResult>;
