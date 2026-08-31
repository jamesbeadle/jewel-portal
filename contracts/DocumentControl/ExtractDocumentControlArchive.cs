using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.DocumentControl;

// Splits a pending zip archive in Document Triage into one new queue item per contained file, so
// each can be previewed and filed individually through the normal filing form. The children copy
// the original's email envelope and project hint; the original resolves as ArchiveExtracted with a
// label saying how many files came out (Restore puts it back if that was a mistake). Nested zips
// land as ordinary child items — each carries its own Extract button, so extraction goes one level
// per click rather than recursing blindly. Returns the newly created items.
public sealed record ExtractDocumentControlArchive(string DocumentControlItemId)
    : ICommand<IReadOnlyList<DocumentControlItem>>;
