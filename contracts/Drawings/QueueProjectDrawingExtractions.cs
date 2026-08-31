using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Drawings;

// The register's "extract all unprocessed": queues the latest live PDF revision of every drawing
// on the project that has never had its metadata extracted and isn't already queued or running.
// Returns how many were queued, for the toast.
public sealed record QueueProjectDrawingExtractions(string ProjectId) : ICommand<int>;
