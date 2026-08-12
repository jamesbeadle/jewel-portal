using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.DocumentControl;

// Returns a discarded Document Control item to the pending queue.
public sealed record RestoreDocumentControlItem(string DocumentControlItemId) : ICommand<DocumentControlItem>;
