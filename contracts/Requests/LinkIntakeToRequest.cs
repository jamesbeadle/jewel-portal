using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Requests;

// Attach a triaged email to an existing request. The handler appends the email body to that
// request's conversation as an inbound, shared message (carrying the email threading ids) and
// marks the intake item Linked.
public sealed record LinkIntakeToRequest(
    string IntakeId,
    string RequestId) : ICommand<IntakeEmail>;
