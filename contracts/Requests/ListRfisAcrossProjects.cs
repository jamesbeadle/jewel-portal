using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Every RFI across the whole project portfolio, for the cross-project RFI dashboard. All statuses
// come back (the dashboard filters Open/Closed/All client-side, like the per-project register).
// Only requests tied to a live project are included — stranded requests belong to triage.
public sealed record ListRfisAcrossProjects() : IQuery<IReadOnlyList<Request>>;
