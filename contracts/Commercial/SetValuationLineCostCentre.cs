using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Commercial;

// Recodes the cost centre a valuation line's value sits against. Exists because variation
// lines are otherwise locked (they mirror approved variation orders), yet the cost centre
// is an allocation attribute — not part of the agreed value — so finance may correct it
// after approval without touching the money.
public sealed record SetValuationLineCostCentre(
    string ValuationLineItemId,
    string CostCode) : ICommand<ValuationLineItem>;
