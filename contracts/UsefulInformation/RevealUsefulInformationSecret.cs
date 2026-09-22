using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.UsefulInformation;

// The one read that answers a credential's value: directors only (UsefulInformationRoles.
// AllowedToReveal), every reveal written to the audit trail with who and when.
public sealed record RevealUsefulInformationSecret(string UsefulInformationNoteId) : IQuery<UsefulInformationSecret>;
