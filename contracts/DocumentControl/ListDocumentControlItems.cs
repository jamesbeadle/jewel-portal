using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.DocumentControl;

// Every Document Control item, newest received first — all statuses in one read; the page splits
// its Queue / Filed / Discarded views client-side (the register is small and one fetch keeps the
// three views consistent with each other).
public sealed record ListDocumentControlItems : IQuery<IReadOnlyList<DocumentControlItem>>;
