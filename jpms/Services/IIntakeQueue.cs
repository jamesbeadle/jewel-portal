using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.Requests;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

// Frontend access to the requests@ mailbox triage queue: list a page of everything still awaiting
// a decision, and the four resolutions a triager can take (claim, link to an existing request,
// create a new request, or discard).
public interface IIntakeQueue
{
    Task<PagedResult<IntakeEmail>> ListOpenAsync(int skip = 0, int take = 25, CancellationToken cancellationToken = default);
    Task<IntakeEmailDetail> GetDetailAsync(string intakeId, CancellationToken cancellationToken = default);
    Task<RequestSuggestion> SuggestAsync(string intakeId, CancellationToken cancellationToken = default);
    Task<IntakeEmail> ClaimAsync(string intakeId, CancellationToken cancellationToken = default);
    Task<IntakeEmail> DiscardAsync(string intakeId, string? notes, CancellationToken cancellationToken = default);
    Task<IntakeEmail> LinkAsync(string intakeId, string requestId, CancellationToken cancellationToken = default);
    Task<Request> CreateRequestAsync(CreateRequestFromIntake command, CancellationToken cancellationToken = default);
}
