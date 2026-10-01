using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Labour;

/// <summary>GET /api/labour/weeks/submissions — the weeks operatives have submitted: those
/// awaiting review first, then the ones answered recently.</summary>
public sealed record ListWorkerWeekSubmissions : IQuery<IReadOnlyList<WorkerWeekSubmission>>;

/// <summary>POST /api/labour/weeks/submissions/{id}/sign-off — a director signs a submitted week
/// off in one step: every waiting day in it is approved at the worker's rate under the budget
/// rules, a day the worker recorded off becomes a recorded absence, the week's sign-off marker is
/// written for the month-end, and the week locks with who signed and when.</summary>
public sealed record SignOffWorkerWeek(string WorkerWeekSubmissionId) : ICommand<WorkerWeekSubmission>;

/// <summary>POST /api/labour/weeks/submissions/{id}/send-back — a director sends a submitted week
/// back with a note; its days reopen for the worker to amend and submit again.</summary>
public sealed record SendBackWorkerWeek(string WorkerWeekSubmissionId, string Note) : ICommand<WorkerWeekSubmission>;
