using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Fetches the full body + attachment list for a single intake email on demand (when a triager
// opens it). The content is pulled live from Microsoft Graph rather than stored, so this is a
// per-open request keyed by the intake id.
public sealed record GetIntakeEmailDetail(string IntakeId) : IQuery<IntakeEmailDetail>;
