using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Boq;

public sealed record UpdateBoqLine(
    string BoqLineItemId,
    string Description,
    string Unit,
    decimal Quantity,
    decimal RateValue,
    string CostCode,
    Discipline Discipline) : ICommand<BoqLineItem>;
