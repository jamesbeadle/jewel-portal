namespace Jewel.JPMS.Models;

// One Useful Information note: a titled piece of free text kept against a project for the office's
// own use — a door code, where the key safe is, skip access arrangements, a site quirk worth
// writing down. Strictly internal: the notes never appear on anything client- or
// subcontractor-facing, and the API gates both reading and managing them to internal roles only.
// Notes are reference material, not work — nothing here is assigned, due or completable; anything
// that needs doing belongs on the To-do tab instead.
public sealed record UsefulInformationNote(
    string UsefulInformationNoteId,
    string ProjectId,
    string Title,
    string Body,
    string CreatedByEmail,
    DateTimeOffset CreatedAt,
    string? UpdatedByEmail,   // who last edited the note; null = never edited since creation
    DateTimeOffset? UpdatedAt);
