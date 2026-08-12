using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.DocumentControl;

// Marks a pending Document Control item Discarded. The file and the email snapshot are kept — the
// Discarded view lists it and Restore puts it back in the queue; nothing is deleted.
public sealed record DiscardDocumentControlItem(string DocumentControlItemId) : ICommand<DocumentControlItem>;
