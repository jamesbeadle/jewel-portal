using Jewel.JPMS.Contracts.Parties;
using Jewel.JPMS.Contracts.Projects;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

/// <summary>
/// Everything correspondence: a party's contact book (the people at a client account or architect
/// practice with their default To/CC/BCC routing), a project's correspondence profile (per-project
/// routing overrides plus ad-hoc recipients such as internal Jewel staff), and the resolved
/// recipients preview for a request. All methods are on-demand async reads/writes — no cached
/// render-time reads, so no fetch-once-per-key bookkeeping is needed here.
/// </summary>
public interface ICorrespondenceStore
{
    Task<IReadOnlyList<PartyContact>> ListPartyContactsAsync(PartyKind kind, string partyId, CancellationToken cancellationToken = default);
    Task<PartyContact> UpsertPartyContactAsync(UpsertPartyContact command, CancellationToken cancellationToken = default);
    Task RemovePartyContactAsync(PartyKind kind, string partyId, string partyContactId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProjectContact>> ListProjectContactsAsync(string projectId, CancellationToken cancellationToken = default);
    Task<ProjectContact> UpsertProjectContactAsync(UpsertProjectContact command, CancellationToken cancellationToken = default);
    Task RemoveProjectContactAsync(string projectId, string contactId, CancellationToken cancellationToken = default);

    /// <summary>The exact To/CC/BCC set an issue or draft would use right now — the same shared
    /// resolution the send paths use, so the preview can never disagree with a real send.</summary>
    Task<RequestRecipientSet> ResolveRequestRecipientsAsync(string requestId, CancellationToken cancellationToken = default);
}
