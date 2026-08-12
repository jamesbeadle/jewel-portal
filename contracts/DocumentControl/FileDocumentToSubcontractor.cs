using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.DocumentControl;

// Files a pending Document Control item onto a subcontractor's record as a versioned compliance
// document (RAMS, Insurance, Drawings / Specifications — Kind is free text like the portal upload).
// The file is copied into the compliance blob store and becomes the current version of its Kind,
// superseding (never replacing) the previous one — exactly the portal upload's behaviour. Returns
// the item, now Filed.
public sealed record FileDocumentToSubcontractor(
    string DocumentControlItemId,
    string SubcontractorId,
    string Kind,
    DateTimeOffset? ExpiresAt) : ICommand<DocumentControlItem>;
