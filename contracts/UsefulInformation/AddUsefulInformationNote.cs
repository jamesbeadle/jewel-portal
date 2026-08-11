using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.UsefulInformation;

// Add a Useful Information note to a project from its Useful Information tab. CreatedByEmail is
// stamped from the signed-in user server-side — never trusted from the client body.
public sealed record AddUsefulInformationNote(
    string ProjectId,
    string Title,
    string Body,
    string CreatedByEmail = "") : ICommand<UsefulInformationNote>;
