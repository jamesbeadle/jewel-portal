using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Projects;

// Sets (or clears, with null) the FD's forecast assumption for a project: roughly how much the
// architect is expected to certify per valuation month (2026-08-13 — "Woodhouse seems to be a
// lot less than that … Abbott will probably land up being more"). Kept as its own command, like
// SetNextValuationDate — the Cash Forecast page edits this one field inline, without
// round-tripping the full UpdateProjectDetails payload. Forecasting only: the Cash Forecast
// claims left-to-claim at this rate instead of spreading it evenly; it never touches
// valuations or invoices.
public sealed record SetExpectedMonthlyValuation(
    string ProjectId,
    decimal? ExpectedMonthlyValuation) : ICommand<Project>;
