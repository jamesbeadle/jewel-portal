using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.UsefulInformation;

// Holds a shared site credential against a note, or removes it (Secret null or blank). The value
// is encrypted at rest and never returned by any list read; it is deliberately its own command,
// not a field on Add/Update, so the connector never carries it — a credential is typed on the
// page, where it is masked, never into a chat. ChangedByEmail is stamped server-side.
public sealed record SetUsefulInformationSecret(
    string UsefulInformationNoteId,
    string? Secret,
    string ChangedByEmail = "") : ICommand<UsefulInformationNote>;
