using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Contracts.UsefulInformation;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

// Useful Information notes: titled free-text reference material kept against a project for the
// office's own use (door codes, key safe locations, site notes) — the project's Useful Information
// tab. Strictly internal: the API gates reads and writes to internal roles, and nothing here ever
// reaches a client-, architect- or subcontractor-facing surface. Notes are looked up, not worked —
// anything with an owner or a due date belongs on the To-do tab instead.
public interface IUsefulInformationStore
{
    Task<IReadOnlyList<UsefulInformationNote>> ListForProjectAsync(string projectId, CancellationToken cancellationToken = default);
    Task<UsefulInformationNote> AddAsync(AddUsefulInformationNote command, CancellationToken cancellationToken = default);
    Task<UsefulInformationNote> UpdateAsync(UpdateUsefulInformationNote command, CancellationToken cancellationToken = default);
    Task<Acknowledgement> DeleteAsync(string usefulInformationNoteId, CancellationToken cancellationToken = default);
}
