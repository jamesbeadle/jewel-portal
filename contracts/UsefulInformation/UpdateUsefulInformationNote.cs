using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.UsefulInformation;

// Full update of a note's title and body. UpdatedByEmail is stamped from the signed-in user
// server-side — never trusted from the client body — and UpdatedAt is stamped by the handler.
public sealed record UpdateUsefulInformationNote(
    string UsefulInformationNoteId,
    string Title,
    string Body,
    string UpdatedByEmail = "") : ICommand<UsefulInformationNote>;
