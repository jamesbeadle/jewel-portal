using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Asks Claude to read a triaged email and propose a draft request (project, type, title, detail and
// the register fields) to pre-fill the "Create new request" form. Pulled on demand when the triager
// clicks "Suggest with AI" — keyed by intake id. The result is advisory only: nothing is created or
// stored until the triager submits CreateRequestFromIntake.
public sealed record SuggestRequestFromIntake(string IntakeId) : IQuery<RequestSuggestion>;
