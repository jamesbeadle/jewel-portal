using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// A staff member takes ownership of a triage item so two people don't work the same email.
// ClaimedByEmail is stamped server-side from the signed-in user, never trusted from the body.
public sealed record ClaimIntakeEmail(
    string IntakeId,
    string ClaimedByEmail = "") : ICommand<IntakeEmail>;
