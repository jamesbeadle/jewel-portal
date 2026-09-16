using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Drawings;

// Re-transcribes extracted revisions into the queryable row tables (dimensions, callouts,
// shapes) from the structure blobs already in storage — no PDF is read again. Meant for the
// revisions extracted before the rows existed (2026-09-16): every succeeded extraction with no
// RowsWrittenAt stamp, on one project or across the portal, is queued rows-only to the worker.
// Force re-transcribes the stamped ones too. Returns how many were queued and how many already
// carried rows, for the reply.
public sealed record RebuildDrawingDataRows(string? ProjectId, bool Force = false) : ICommand<DrawingDataRowsRebuild>;

public sealed record DrawingDataRowsRebuild(int Queued, int AlreadyTranscribed);
