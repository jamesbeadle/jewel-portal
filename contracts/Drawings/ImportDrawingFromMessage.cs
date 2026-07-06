using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Drawings;

// Save an email attachment into a project's drawings: downloads the attachment from the mailbox,
// stores it in the drawing blob store, and records an Unapproved revision — finding the drawing by
// its code within the project, or registering it first when the code is new. This is how drawings
// arrive from triage (architects issue revisions by email); approval then follows the normal
// drawings workflow. Returns the drawing the revision landed on.
public sealed record ImportDrawingFromMessage(
    string ProjectId,
    string MessageId,
    string AttachmentId,
    string DrawingCode,
    string Title,
    string RevisionLabel) : ICommand<Drawing>;
