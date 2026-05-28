using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Boq;

public sealed record RemoveBoqLine(string BoqLineItemId) : ICommand<Acknowledgement>;
