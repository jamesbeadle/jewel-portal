using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Drawings;

// A revision's data view: extraction status plus, once succeeded, its markups and per-page text.
// Null when the revision has never been queued.
public sealed record GetDrawingExtraction(string DrawingRevisionId) : IQuery<DrawingExtractionView?>;
