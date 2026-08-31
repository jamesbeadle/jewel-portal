using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Drawings;

// Queues one drawing revision for data extraction (Bluebeam markups + PDF text layer). The work
// itself runs on the worker — this returns the row already stamped Queued, and the drawing page
// polls the extraction query by hand (its Refresh button) until the worker moves it on. Queueing
// a revision that already extracted re-runs it: Force rides on the queue message so the worker
// knows the overwrite is meant.
public sealed record QueueDrawingExtraction(
    string ProjectId,
    string DrawingId,
    string DrawingRevisionId) : ICommand<DrawingExtraction>;
