using Jewel.JPMS.Contracts.Cqrs;

namespace Jewel.JPMS.Contracts.Commercial;

public sealed record RemoveValuationLineItem(string ValuationLineItemId) : ICommand<Acknowledgement>;
